# Changelog

All notable changes to git-metrics will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-11

### Added
- Daily metrics feature allowing users to analyze Git objects within specific date ranges
- New command-line flags: `--daily-start` and `--daily-end` for date range filtering
- Date validation ensuring proper YYYY-MM-DD format and logical date ordering
- Simplified output mode for daily metrics showing:
  - Total commits, trees, and blobs within the date range
  - Object size and on-disk size totals
  - Top 10 largest files from the date range
- Documentation updates in README.md for the new daily metrics feature

### Features
- Repository metrics analysis with year-by-year growth statistics
- Future growth projections based on historical trends
- Directory structure analysis with size impact indicators
- Identification of largest files in the repository
- File extension distribution analysis
- Contributor statistics showing top committers and authors over time
- Rate of changes analysis with commit patterns
- Progress indicators for long-running operations
- Debug mode for troubleshooting

[1.0.0]: https://github.com/k-y/git-metrics/releases/tag/v1.0.0
