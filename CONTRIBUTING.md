# Contributing to Open76

Thank you for your interest in contributing to Open76.
This project is an open-source Unity reimplementation of Interstate '76, and contributions are welcome from anyone who wants to help improve file format parsing, level loading, simulation, or related gameplay systems.

## How to contribute

1. Fork the repository and create a branch for your work.
   - Use a descriptive branch name, for example `fix/mission-parser`, `feature/vr-support`, or `doc/update-readme`.
2. Review open issues and existing discussions.
   - If you plan to work on an issue, leave a comment to let others know you are working on it.
3. Make your changes in a clean branch.
   - Keep commits small and focused.
   - Use meaningful commit messages.
4. Test your changes in Unity before submitting.
   - Open `Level.unity` or the relevant scene and verify your changes work as expected.
5. Submit a pull request.
   - Reference the issue number if applicable.
   - Describe the change, the problem it solves, and any testing steps.

## Pull request expectations

- Keep changes scoped to a single issue or feature.
- Include any new assets or script changes needed for the fix.
- Avoid committing unnecessary generated or local Unity files.
- If you change project settings, explain why in the PR description.

## Coding guidelines

- Follow the existing Unity/C# code style in the repository.
- Prefer clear, self-documenting names for classes, methods, and fields.
- Keep architecture consistent with the project’s data-driven parsers and singleton managers.
- Add comments for non-obvious behavior, especially for reverse-engineered file formats.

## Issue workflow

- Search existing issues before opening a new one.
- Provide enough detail to reproduce the problem.
- Tag the issue as `bug`, `feature`, or `question` where appropriate.

## Notes

- This project is licensed under GPLv3.
- Contributions should be compatible with the project license.
- The repository is not affiliated with or endorsed by Activision.

## Want to help but not code?

- Report bugs or suggest improvements in Issues.
- Help improve documentation or README content.
- Share insights on reverse engineering or Unity integration.
