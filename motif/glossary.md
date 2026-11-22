# Glossary

The words this project uses, and what they mean. People and agents use these terms in code, docs, issues and discussion, and avoid the words listed under **Avoid**. A new concept gets an entry, marked *proposed*, in the same change that introduces it. An entry with no status is agreed.

The first entries came from the sketch ([sketch.motifsketch.json](sketch.motifsketch.json)): **Sketch** gives the id of the element or port each one comes from. The format is described in the [Motif Glossary design note](https://github.com/motif-dotnet/.github/blob/main/design/glossary.md).

## Code Folders

A Motif repo's folder on the Reviewer's computer, opened or dropped in, with the Stryker reports in its StrykerOutput/ folder.

- **Kind:** storage
- **Status:** proposed
- **Sketch:** `b0b1a005`

## For opening folders

Reads the code in a folder the Reviewer opens or drops in, and the latest Stryker report in its StrykerOutput/ folder.

- **Kind:** port
- **Status:** proposed
- **Sketch:** `b0b1c003`

## For opening repos

Reads a public repo's code from its default branch, and its latest Stryker report from its Pages site if it has one.

- **Kind:** port
- **Status:** proposed
- **Sketch:** `b0b1c002`

## GitHub

Public repos' code, read from the default branch.

- **Kind:** external system
- **Status:** proposed
- **Sketch:** `b0b1a003`

## GitHub Pages

Where a repo's CI publishes its latest Stryker report, motif/mutation-report.json.

- **Kind:** external system
- **Status:** proposed
- **Sketch:** `b0b1a004`

## Reviewer

Opens or drops a Motif repo's folder, or names a public repo, and looks over its architecture and how well its specs check it.

- **Kind:** actor
- **Status:** proposed
- **Sketch:** `b0b1a002`

## User

- **Kind:** port
- **Status:** proposed
- **Sketch:** `b0b1c001`
