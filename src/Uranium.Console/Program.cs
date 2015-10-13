﻿namespace Uranium.Console
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using ColoredConsole;
    using Uranium.Model;
    using Uranium.Model.Octokit;

    internal static class Program
    {
        public static void Main()
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            var client = GitHubClientFactory.Create(
                typeof(Program).Namespace,
                Environment.GetEnvironmentVariable("OCTOKIT_GITHUBUSERNAME"),
                Environment.GetEnvironmentVariable("OCTOKIT_GITHUBPASSWORD"));

            var committerGroups = await new CrappyCommitterGroupService("groups.txt", "Particular").Get("Particular");

            var repositoryService = new RepositoryService(client);
            var repositories = (await Task.WhenAll(committerGroups
                    .SelectMany(@group => @group.RepositoryList.Select(id => id.Owner))
                    .Distinct()
                    .Select(owner => repositoryService.Get(owner))))
                .SelectMany(_ => _)
                .ToList();

            var ungroupedRepositories = repositories
                .Where(repository => !committerGroups.Any(group => group.RepositoryList.Contains(repository.Id)));

            foreach (var repo in ungroupedRepositories.OrderBy(_ => _))
            {
                ColorConsole.WriteLine(
                    "* **".White(), $"Repo '{repo.Id.Owner}/{repo.Id.Name}' is not grouped!".Red(), "**".White());
            }

            TsvContributionsRepository.Save(await ContributionService.Get(committerGroups, new CommitService(client)));
        }
    }
}
