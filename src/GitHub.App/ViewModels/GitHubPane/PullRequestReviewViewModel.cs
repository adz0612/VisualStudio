﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using GitHub.Logging;
using GitHub.Models;
using ReactiveUI;
using Serilog;

namespace GitHub.ViewModels.GitHubPane
{
    /// <summary>
    /// View model for displaying details of a pull request review.
    /// </summary>
    [Export(typeof(IPullRequestReviewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PullRequestReviewViewModel : ViewModelBase, IPullRequestReviewViewModel
    {
        static readonly ILogger log = LogManager.ForContext<PullRequestReviewViewModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PullRequestReviewViewModel"/> class.
        /// </summary>
        /// <param name="localRepository">The local repository.</param>
        /// <param name="owner">The pull request's repository owner.</param>
        /// <param name="pullRequest">The pull request model.</param>
        /// <param name="pullRequestReviewId">The pull request review ID.</param>
        [ImportingConstructor]
        public PullRequestReviewViewModel(
            ILocalRepositoryModel localRepository,
            string owner,
            IPullRequestModel pullRequest,
            long pullRequestReviewId)
        {
            LocalRepository = localRepository;
            RemoteRepositoryOwner = owner;
            Model = GetModel(pullRequest, pullRequestReviewId);
            PullRequestModel = pullRequest;
            Body = string.IsNullOrWhiteSpace(Model.Body) ? null : Model.Body;
            StateDisplay = ToString(Model.State);

            var comments = new List<IPullRequestReviewFileCommentViewModel>();
            var outdated = new List<IPullRequestReviewFileCommentViewModel>();

            foreach (var comment in PullRequestModel.ReviewComments)
            {
                if (comment.PullRequestReviewId == pullRequestReviewId)
                {
                    var vm = new PullRequestReviewFileCommentViewModel(comment);

                    if (comment.Position.HasValue)
                        comments.Add(vm);
                    else
                        outdated.Add(vm);
                }
            }

            FileComments = comments;
            OutdatedFileComments = outdated;

            HasDetails = Body != null ||
                FileComments.Count > 0 ||
                OutdatedFileComments.Count > 0;
            IsExpanded = HasDetails && CalculateIsLatest(pullRequest, Model);
        }

        /// <inheritdoc/>
        public ILocalRepositoryModel LocalRepository { get; private set; }

        /// <inheritdoc/>
        public string RemoteRepositoryOwner { get; private set; }

        /// <inheritdoc/>
        public IPullRequestReviewModel Model { get; }

        /// <inheritdoc/>
        public IPullRequestModel PullRequestModel { get; }

        /// <inheritdoc/>
        public string Body { get; }

        /// <inheritdoc/>
        public string StateDisplay { get; }

        /// <inheritdoc/>
        public bool IsExpanded { get; }

        /// <inheritdoc/>
        public bool HasDetails { get; }

        /// <inheritdoc/>
        public IReadOnlyList<IPullRequestReviewFileCommentViewModel> FileComments { get; }

        /// <inheritdoc/>
        public IReadOnlyList<IPullRequestReviewFileCommentViewModel> OutdatedFileComments { get; }

        /// <inheritdoc/>
        public ReactiveCommand<Unit> OpenComment { get; }

        static bool CalculateIsLatest(IPullRequestModel pullRequest, IPullRequestReviewModel model)
        {
            return !pullRequest.Reviews.Any(x =>
                x.User.Login == model.User.Login &&
                x.SubmittedAt > model.SubmittedAt);    
        }

        static IPullRequestReviewModel GetModel(IPullRequestModel pullRequest, long pullRequestReviewId)
        {
            var result = pullRequest.Reviews.FirstOrDefault(x => x.Id == pullRequestReviewId);

            if (result == null)
            {
                throw new KeyNotFoundException(
                    $"Unable to find review {pullRequestReviewId} in pull request #{pullRequest.Number}");
            }

            return result;
        }

        static string ToString(PullRequestReviewState state)
        {
            switch (state)
            {
                case PullRequestReviewState.Approved:
                    return "approved";
                case PullRequestReviewState.ChangesRequested:
                    return "requested changes";
                case PullRequestReviewState.Commented:
                case PullRequestReviewState.Dismissed:
                    return "commented";
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
