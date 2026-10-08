## Releasing BitShelter

This document describes how to make a release, and how to set up code signing with [SignPath Foundation](https://signpath.org/) later. Releases are not code-signed at present (see [Code signing](README.md#code-signing) in the README).

### Make a release

1. Set `<Version>` in `Directory.Build.props`, for example `0.2.1`.
2. Add a section `### [0.2.1] - YYYY-MM-DD` to `CHANGELOG.md`.
3. Commit, then push a tag that matches the version:
   - `git tag v0.2.1`
   - `git push origin v0.2.1`
4. The **Release** workflow (`.github/workflows/release.yml`) then does these steps:
   - It checks that the tag matches `<Version>`.
   - It builds, runs the tests, and builds the MSI on a GitHub-hosted runner.
   - It uploads the unsigned MSI as a workflow artifact.
   - If signing is set up, it sends the MSI to SignPath and waits for the signed MSI.
   - It creates a GitHub release with the MSI. The CHANGELOG section of the version is the release text.

You can also start the workflow by hand (**Actions** > **Release** > **Run workflow**). A run that a tag did not start builds and signs, but creates no release.

### Set up code signing (optional, not set up)

SignPath Foundation signs only projects that already have a release. If you set up signing, also replace the **Code signing** section of the README with the code signing policy that SignPath Foundation requires (who commits, reviews, and approves).

1. Make sure that every member of the project uses multi-factor authentication on GitHub. SignPath Foundation requires it.
2. Apply at [signpath.org](https://signpath.org/apply) and follow their review.
3. When SignPath accepts the project, in SignPath:
   - Link the predefined trusted build system **GitHub.com** to the project.
   - Install the [SignPath GitHub App](https://github.com/apps/signpath) for this repository.
   - Create an artifact configuration. Use the example below as a starting point, and compare it with the configuration that SignPath generates when you upload an unsigned MSI artifact.
   - Create a signing policy that requires manual approval.
4. In GitHub, open **Settings** > **Secrets and variables** > **Actions**:
   - Add the secret `SIGNPATH_API_TOKEN` (an API token of a SignPath user with submitter permission).
   - Add the variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, and `SIGNPATH_SIGNING_POLICY_SLUG`.

The signing step runs only when `SIGNPATH_ORGANIZATION_ID` is set. Without it, the workflow publishes an unsigned MSI.

#### Artifact configuration (starting point)

The workflow uploads the MSI in a zip file, so the root element is `<zip-file>`. Sign only BitShelter's own binaries. Third-party DLLs stay unsigned, as SignPath Foundation requires. The metadata restrictions use the values from `Directory.Build.props`.

```xml
<artifact-configuration xmlns="http://signpath.io/artifact-configuration/v1">
  <parameters>
    <parameter name="version" required="true" />
  </parameters>
  <zip-file>
    <msi-file path="BitShelter-*-x64.msi">
      <pe-file-set product-name="BitShelter" product-version="${version}" company-name="BitShelter">
        <include path="BitShelter.Agent.exe" />
        <include path="BitShelter.Agent.dll" />
        <include path="BitShelter.Service.exe" />
        <include path="BitShelter.Service.dll" />
        <include path="BitShelter.Common.dll" />
        <for-each>
          <authenticode-sign />
        </for-each>
      </pe-file-set>
      <authenticode-sign />
    </msi-file>
  </zip-file>
</artifact-configuration>
```

The workflow sends the `version` parameter (the `<Version>` value).
