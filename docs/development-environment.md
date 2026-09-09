# Windows development environment

This guide sets up this eShop fork on a clean Windows 11 PC. Run commands in Windows PowerShell from the repository root unless a step says otherwise. Installation may require administrator approval and restarts; normal development uses a regular terminal.

## Architecture and prerequisites

.NET, VS Code, Aspire CLI, and the eShop application processes run on Windows. Docker Desktop uses its WSL2 backend and Linux containers for infrastructure such as PostgreSQL, Redis, and RabbitMQ. Windows processes and Linux containers participate together; eShop is not simply running inside Ubuntu. Ubuntu is a WSL distribution, while Docker Desktop manages its own Linux backend.

Use a Windows 11 PC with hardware virtualization enabled and sufficient memory and disk space for the SDK, container images, and browser downloads. Follow the [Docker Desktop Windows requirements](https://docs.docker.com/desktop/setup/install/windows-install/). Keep the checkout on the Windows filesystem and open it in local VS Code for this workflow.

## Validated toolchain snapshot

This setup was validated on **2026-09-07** with the following versions. Compatible newer versions may work but were not validated in this record. Installer defaults can change; use official version archives when reproducing the snapshot exactly.

| Tool | Validated version |
| --- | --- |
| Git | 2.55.0.windows.5 |
| GitHub CLI | 2.100.0 |
| .NET SDK | 10.0.400 |
| Node.js | 24.19.0 |
| npm | 11.17.0 |
| Docker | 29.7.2 |
| Aspire CLI | 13.5.3 |
| VS Code | 1.136.1 |
| Codex CLI | 0.153.4 |

The Docker version above is the Docker CLI/Engine version, not a Docker Desktop installer version. `global.json` requests .NET SDK `10.0.302` with `latestFeature` roll-forward, allowing the recorded `10.0.400`. Install the SDK, not only the runtime. The web workflow does not require MAUI workloads or a separate Aspire workload.

## Install and verify

### Windows tools

Install the Windows packages matching your processor architecture from these official sources. Enable PATH integration where offered, then reopen PowerShell and VS Code so they inherit the updated PATH.

| Tool | Installation guidance | Verification |
| --- | --- | --- |
| Git | Use the [Git for Windows installer](https://git-scm.com/install/windows). | `git --version` |
| GitHub CLI | Use the Windows installer linked from [GitHub CLI](https://cli.github.com/). | `gh --version` |
| .NET 10 SDK | Select a Windows SDK installer from [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). | `dotnet --info` and `dotnet --list-sdks` |
| Node.js and npm | Use the [official Node.js Windows installer](https://nodejs.org/en/download), which includes npm. | `node --version` and `npm --version` |
| VS Code | Use the [Windows user installer](https://code.visualstudio.com/docs/setup/windows) with Add to PATH enabled. | `code --version` |

If reproducing the snapshot requires updating the bundled npm, use its established package installation syntax after installing Node.js:

```powershell
npm install --global npm@11.17.0
npm --version
```

### WSL2 and Docker Desktop

In an **administrator PowerShell**, install WSL with Ubuntu, then restart Windows when prompted. Complete Ubuntu's first-launch account setup privately. See [Microsoft's WSL installation guide](https://learn.microsoft.com/en-us/windows/wsl/install).

```powershell
wsl --install -d Ubuntu
```

After restarting, verify and update WSL. Ubuntu should report version 2; convert it if it reports version 1.

```powershell
wsl --update
wsl --status
wsl --list --verbose
# Only if Ubuntu is listed as version 1:
wsl --set-version Ubuntu 2
```

Install and launch [Docker Desktop for Windows](https://docs.docker.com/desktop/setup/install/windows-install/). Select the WSL2 backend, enable **Use the WSL 2 based engine** in Settings if necessary, and use Linux containers. Ubuntu integration is only needed if you also want to invoke Docker from Ubuntu. Wait until the engine is running, then verify from Windows PowerShell:

```powershell
docker --version
docker version
docker info --format '{{.OSType}}'
docker run --rm hello-world
```

Expect both client and server information, an OS type of `linux`, and a successful hello-world message.

### Aspire CLI and editor extensions

Install Aspire using one method from the [official CLI installation guide](https://aspire.dev/get-started/install-cli/). For the recorded version, the .NET global-tool method is:

```powershell
dotnet tool install --global Aspire.Cli --version 13.5.3
aspire --version
```

In VS Code's Extensions view, install **C#** and **C# Dev Kit**, both published by Microsoft. Follow [C# setup](https://code.visualstudio.com/docs/csharp/get-started). Install/enable **GitHub Copilot Chat**, published by GitHub, and complete GitHub sign-in through the editor's [Copilot setup flow](https://code.visualstudio.com/docs/setup/copilot). Confirm that Chat opens and the required account access is available; GitHub CLI authentication does not replace editor sign-in.

Verify installed extension versions, then confirm the C# extensions are enabled in the current workspace:

```powershell
code --list-extensions --show-versions
```

### OpenAI Codex CLI

Use the npm installation option in the [official Codex CLI documentation](https://developers.openai.com/codex/cli/). To reproduce the recorded version:

```powershell
npm install --global @openai/codex@0.153.4
codex --version
codex
```

Complete the interactive sign-in flow. Codex and Copilot are development aids, not prerequisites for running eShop. Keep authentication material outside the repository.

## GitHub and repository setup

Authenticate using the browser flow, selecting GitHub.com and HTTPS:

```powershell
gh auth login
gh auth status
```

Fork `dotnet/eShop` through GitHub's [fork workflow](https://docs.github.com/en/get-started/exploring-projects-on-github/contributing-to-a-project), or use your existing fork. Replace `<github-user>` below with your GitHub account name before running the commands. Run the clone command from your chosen parent directory:

```powershell
git clone https://github.com/<github-user>/eShop.git
Set-Location eShop
git remote add upstream https://github.com/dotnet/eShop.git
git remote -v
git status --short
code .
```

`origin` must point to your fork; `upstream` must point to `https://github.com/dotnet/eShop.git`. If either remote already exists but is incorrect, use the corresponding command after inspecting it:

```powershell
git remote set-url origin https://github.com/<github-user>/eShop.git
git remote set-url upstream https://github.com/dotnet/eShop.git
```

In C# Dev Kit's Solution Explorer, open **`eShop.slnx`** explicitly using **.NET: Open Solution** from the Command Palette. Wait for project loading and restore, then verify F12/Go to Definition on a project symbol. `eShop.Web.slnf` selects the server/test subset for terminal validation.

## Local configuration

Check HTTPS trust and install the locked JavaScript dependencies and Chromium browser required by `README.md` and `playwright.config.ts`:

```powershell
dotnet dev-certs https --check --trust
# If the check fails because a trusted certificate is missing:
dotnet dev-certs https --trust
npm ci
npx playwright install chromium
```

Create a root `.env` only if it does not already exist. This writes variable names with empty values, never example credentials:

```powershell
if (-not (Test-Path -LiteralPath .env)) {
    @('USERNAME1=', 'PASSWORD=') | Set-Content -LiteralPath .env -Encoding utf8
}
git check-ignore .env
code .env
```

Privately fill in credentials for a valid local eShop test account, using the local identity seed configuration as needed. `e2e/login.setup.ts` requires both variables; blank values are insufficient. `playwright.config.ts` loads `.env` from the repository root, and `.gitignore` already ignores it. Never commit `.env`, credentials, dashboard tokens, or secrets. Do not paste credential files or authenticated dashboard links into documentation or AI prompts.

## Run and validate

Start Docker Desktop first. From the repository root:

```powershell
docker run --rm hello-world
aspire run
```

`aspire.config.json` selects `src/eShop.AppHost/eShop.AppHost.csproj`. Wait for resource readiness and open the storefront through the local Aspire dashboard. Keep the dashboard's authenticated link private. Check that catalog browsing and sign-in work.

In a second PowerShell terminal at the repository root:

```powershell
dotnet test --solution eShop.Web.slnf
npm run test:e2e
```

Playwright uses the storefront endpoint configured in `playwright.config.ts`; locally it reuses an existing server or starts AppHost automatically. Docker must remain available either way. Avoid running multiple manual AppHost instances. The previously established baseline was **122 .NET tests and 4 Playwright tests passing**; compare new results with the current checkout rather than assuming counts never change.

Press **Ctrl+C** in the `aspire run` terminal and wait for shutdown. Persistent infrastructure containers may remain running by design; inspect them in Docker Desktop if you need to stop them. Do not delete volumes as routine shutdown.

## Troubleshooting observed setup issues

| Symptom | Action |
| --- | --- |
| Virtualization disabled or WSL2 cannot start | Check Task Manager's CPU virtualization status. Enable virtualization in firmware and **Virtual Machine Platform** in Windows Features, then restart. Recheck `wsl --status` and `wsl --list --verbose`; follow [Microsoft's WSL troubleshooting](https://learn.microsoft.com/en-us/windows/wsl/troubleshooting). |
| `npm.ps1` blocked by execution policy | Inspect policy scopes and set `RemoteSigned` for `CurrentUser` as shown below. Do not globally bypass policy. If organizational Group Policy takes precedence, follow administrator guidance. |
| Docker command unrecognized | Finish Docker Desktop installation, reopen terminals and VS Code, and check `Get-Command docker`. |
| Docker daemon unavailable | Launch Docker Desktop, wait for the engine, confirm Linux containers/WSL2 mode, then run `docker version` and hello-world again. |
| Aspire initial build/startup timeout | First startup may need package restore, image downloads, migrations, and seeding. Inspect the first failure in terminal/resource logs and confirm Docker readiness. Stop an incomplete run with Ctrl+C, then retry `aspire run` after dependencies are ready. Playwright currently allows three minutes locally; investigate readiness before changing timeouts. |
| Local HTTPS certificate errors | Run the certificate check and trust commands above; accept the Windows trust prompt and restart affected browsers/processes. Avoid disabling certificate validation globally. See [development certificate guidance](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs). |
| Playwright reports missing `USERNAME1`/`PASSWORD` | Run from the repository root and populate both variables in the ignored `.env`. Confirm the account can sign in manually; do not print its password to diagnose the failure. |
| C# Dev Kit shows No Solution or F12 fails | Open the repository folder locally, enable C# and C# Dev Kit, explicitly load `eShop.slnx`, and wait for restore. Check the C#/.NET Output channels and `dotnet --info` for SDK or project-load failures; reload the VS Code window after fixing them. |
| Catalog OpenAPI JSON changes after build/test | Inspect `src/Catalog.API/Catalog.API.json` and `src/Catalog.API/Catalog.API_v2.json`. Restore only those files if the changes are unintended generated output; preserve deliberate contract edits. |
| `ASPIRE010` warning | It was non-blocking in the validated setup. Read its full diagnostic and track the underlying issue; do not casually suppress it or assume unrelated errors are harmless. |

For the PowerShell policy issue, use a regular terminal. See [Set-ExecutionPolicy documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy):

```powershell
Get-ExecutionPolicy -List
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
npm --version
```

Inspect generated changes before any selective restore:

```powershell
git diff -- src/Catalog.API/Catalog.API.json src/Catalog.API/Catalog.API_v2.json
# Only after confirming these working-tree changes are unintended:
git restore -- src/Catalog.API/Catalog.API.json src/Catalog.API/Catalog.API_v2.json
```

## Final clean-baseline checklist

From the repository root, verify tool versions, authentication, infrastructure, and remotes:

```powershell
git --version
gh --version
dotnet --version
node --version
npm --version
docker --version
aspire --version
code --version
codex --version
code --list-extensions --show-versions
gh auth status
wsl --status
wsl --list --verbose
docker version
docker info --format '{{.OSType}}'
docker run --rm hello-world
git remote -v
git check-ignore .env
dotnet dev-certs https --check --trust
git status --short
aspire run
```

Confirm resource readiness and storefront access. In a second terminal, run validation, inspect any generated changes, and confirm Git status:

```powershell
dotnet test --solution eShop.Web.slnf
npm run test:e2e
git diff --stat
git status --short
```

A clean baseline has passing validation and no output from `git status --short`. Investigate every unexpected change; do not use a blanket reset or clean to obtain that state. Finish with Ctrl+C in the AppHost terminal. These commands are instructions for a new setup; no application run or tests were performed to author this documentation-only change.
