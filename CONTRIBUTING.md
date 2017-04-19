# How to contribute
So, you'd like to contribute and make OctoPack even better? For this, we :heart: you!

We're happy to accept most contributions to help make OctoPack better but we're keen to maintain it's simplicity and we’re very cautious about breaking changes.

Things that will increase the chance that your pull request will be accepted:
- Get in touch via [support forum](http://help.octopusdeploy.com) to let us know what you're working on.
- Follow existing code conventions.
- Include unit tests that would otherwise fail without your code, but pass with it.

## Getting started

[Fork](https://help.github.com/articles/fork-a-repo), then clone your fork of the repository:

`git clone git@github.com:{yourusernamehere}/OctoPack.git`

Then, run the `build.ps1` in the root of the repository to ensure the solution builds and all tests pass. 
_**NB: The build script needs an update to run all the tests as it doesn't at the moment**_

## Making changes

When you're ready to make a change:
 - Create a branch of the `master` branch. To minimize the risk of causing conflicts further down the track.
 - Make your changes, if the changes are non-trivial, make small commits with descriptive commit messages (rather than one huge commit) to make it easier to review your PR, and decrease the chance of your PR being declined.
 - If you are adding new features, add tests to the `OctoPack.Tests` project and make sure they pass before submitting your PR.
 - Run the `build.ps1` script in the root of the repository to ensure the project builds. 
 
## Submitting changes
When you've finished making your changes and all the tests pass. You can publish your branch from the command line:

`git push origin {mybranch}`

Once you've published your branch to GitHub, [open a pull request](https://help.github.com/articles/using-pull-requests) against it.

If you want to start a conversation with the Octopus Deploy team on a PR, you can:
 - Prefix your PR title with `[WIP]`
 - Use checklists to indicate tasks that you have completed, and still have left to complete
 - Add comments to the PR with questions you may have, or you would like suggestions on.

If the changes you are making are related to an issue against OctoPack, don't forget to reference the issue number in the PR.

Finally, let us

**Happy contributing!**
