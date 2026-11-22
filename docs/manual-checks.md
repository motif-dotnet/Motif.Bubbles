# Manual checks

Every spec runs the real adapters over fake Fittings, so the real Fittings are checked by hand. The browser specs (`Checks/Motif.Bubbles.BrowserSpecs`, decision 8) cover the layout, the side panel, the real folder picker and a repo link, in Edge, but with GitHub standing in, and not dropping or Firefox. Run a Fitting's checks before a release when it has changed, in Chrome, and in Firefox when the browser's side of it has changed. If a fake Fitting turns out to behave differently from the real one, tighten the contract in its Seam's doc comments, and fix the fake.

## Local Folders (`JsInteropLocalFolders`, `wwwroot/folders.js`)

- [ ] **Open** shows the folder picker. Picking this repo's root folder draws its bubbles, with rings when `StrykerOutput/` holds a report.
- [ ] Picking a folder with no `StrykerOutput/` draws the bubbles with a note that there are no mutation rings.
- [ ] Closing the picker changes nothing.
- [ ] Dropping the repo's folder anywhere on the page draws it, as picking it does.
- [ ] Dropping files loose draws them as "Files dropped".
- [ ] Dropping text, such as a selection from another page, does nothing, and the page doesn't navigate away.
- [ ] A large repo opens without the page freezing, and `bin`, `obj`, `node_modules`, `.git` and `.vs` aren't read.

## GitHub (the browser's `HttpClientHandler`)

- [ ] **Repo** with `motif-dotnet/Motif.Glossary` draws its bubbles with rings from its Pages site, and the address carries `#repo=motif-dotnet/Motif.Glossary`.
- [ ] `motif-dotnet/Motif.Bubbles` draws itself, ringed from its own report.
- [ ] A public repo with no report on its Pages site, such as `motif-dotnet/Motif.Is`, draws its bubbles with a note that there are no mutation rings.
- [ ] A repo that doesn't exist says it isn't a public GitHub repo.
- [ ] With the network off (DevTools, Network, Offline), a repo says GitHub couldn't be reached.
