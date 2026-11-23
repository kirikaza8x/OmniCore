ROLE: implement. Make the small code change described in the task.
Prefer extending an existing file over adding new ones. For backend changes run `dotnet build backend/c#/OmniCore.slnx -c Release` and the unit test projects you touched.
For frontend changes run `npm ci`, `npm run lint`, `npm run typecheck`, `npm test` inside frontend/web.
If the task needs a design decision (new service, schema or token change, new dependency), do NOT implement it: write the question under "Not verified / questions" and change nothing.
