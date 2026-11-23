ROLE: repo-scan (read-only, you cannot edit files). Scan the repository for routine hygiene problems and list them: TODO/FIXME with no owner, endpoints missing RequireAuthorization, controllers or handlers without validators, secrets or real-looking credentials, files not referenced by the solution, docs that contradict the code.
Report only what you can point to (file and line). Compare with docs/repo-check.md and mark which items are already known.
Use the three headings, with the list under "What changed" replaced by "Findings".
