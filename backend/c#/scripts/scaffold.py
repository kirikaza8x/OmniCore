#!/usr/bin/env python3
"""Scaffold a new OmniCore service from backend/c#/scripts/templates/service (see docs/new-service.md).

    python3 scaffold.py Notification --route notifications --port 5003 --consumes Auth

Writes the five service projects and the three test projects, adds them to OmniCore.slnx, adds the database to
infra/docker/scripts/init-dbs.sql and the .env.example, adds the tests to the CI loops, and prints the compose and nginx
snippets that still need a human decision. Existing files are never overwritten. Needs only Python 3.8+.
"""
import argparse, pathlib, re, sys

HERE = pathlib.Path(__file__).resolve().parent
TEMPLATES = HERE / "templates" / "service"
BACKEND = HERE.parent                      # backend/c#
REPO = BACKEND.parent.parent


def render(text, ctx):
    for key, value in ctx.items():
        text = text.replace(key, value)
    return text


def expand_consumed(text, consumed):
    """A line that contains __Consumed__ is repeated once per consumed service (and dropped when there are none)."""
    out = []
    for line in text.splitlines(keepends=True):
        if "__Consumed__" in line:
            out.extend(line.replace("__Consumed__", c) for c in consumed)
        else:
            out.append(line)
    return "".join(out)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("name", help="PascalCase service name, e.g. Notification")
    ap.add_argument("--route", help="URL segment after /api/v1/ (default: lower-case name + 's')")
    ap.add_argument("--schema", help="PostgreSQL schema (default: lower-case name; avoid reserved words like user)")
    ap.add_argument("--port", type=int, default=5099, help="local dev port for launchSettings / published compose port")
    ap.add_argument("--consumes", nargs="*", default=[], help="services whose Contracts this one references, e.g. Auth")
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()

    if not re.fullmatch(r"[A-Z][A-Za-z0-9]+", a.name):
        sys.exit("name must be PascalCase letters/digits, e.g. Notification")
    lower = a.name.lower()
    ctx = {
        "__Service__": a.name, "__service__": lower, "__SERVICE__": lower.upper(),
        "__route__": a.route or lower + "s", "__schema__": a.schema or lower, "__devport__": str(a.port),
    }
    for c in a.consumes:
        if not (BACKEND / f"OmniCore.Services.{c}" / f"OmniCore.Services.{c}.Contracts").is_dir():
            sys.exit(f"--consumes {c}: no such service Contracts project")

    target_service = BACKEND / f"OmniCore.Services.{a.name}"
    if target_service.exists():
        sys.exit(f"{target_service} already exists; refusing to overwrite")

    written = []
    for src in sorted(TEMPLATES.rglob("*")):
        if not src.is_file():
            continue
        rel = src.relative_to(TEMPLATES).as_posix()
        top, _, rest = rel.partition("/")
        base = BACKEND if top == "src" else BACKEND / "tests"
        if top == "tests":
            dest_rel = rest
        else:
            dest_rel = rest
        dest = base / render(dest_rel, ctx)
        if dest.name.endswith(".tmpl"):
            dest = dest.with_name(dest.name[:-5])
            text = render(expand_consumed(src.read_text(encoding="utf-8"), a.consumes), ctx)
        else:
            text = src.read_text(encoding="utf-8")
        if dest.exists():
            sys.exit(f"{dest} exists; refusing to overwrite")
        written.append((dest, text))

    for dest, text in written:
        print(("would write " if a.dry_run else "write ") + str(dest.relative_to(REPO)))
        if not a.dry_run:
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_text(text, encoding="utf-8", newline="\n")

    # --- solution file -------------------------------------------------------------------------------------------
    slnx = BACKEND / "OmniCore.slnx"
    s = slnx.read_text(encoding="utf-8")
    if f"OmniCore.Services.{a.name}/" not in s:
        projects = "".join(
            f'    <Project Path="OmniCore.Services.{a.name}/OmniCore.Services.{a.name}.{l}/OmniCore.Services.{a.name}.{l}.csproj" />\n'
            for l in ("Api", "Application", "Contracts", "Domain", "Infrastructure"))
        folder = f'  <Folder Name="/OmniCore.Services.{a.name}/">\n{projects}  </Folder>\n'
        s = s.replace('  <Folder Name="/OmniCore.Shared/">', folder + '  <Folder Name="/OmniCore.Shared/">', 1)
        tests = "".join(
            f'    <Project Path="tests/OmniCore.Services.{a.name}.{t}/OmniCore.Services.{a.name}.{t}.csproj" />\n'
            for t in ("Application.Tests", "Domain.Tests", "IntegrationTests"))
        s = s.replace("  </Folder>\n</Solution>", tests + "  </Folder>\n</Solution>", 1)
        print("patch OmniCore.slnx")
        if not a.dry_run:
            slnx.write_text(s, encoding="utf-8", newline="\n")

    # --- database ------------------------------------------------------------------------------------------------
    db = f"omnicore_{lower}_db"
    sql = REPO / "infra/docker/scripts/init-dbs.sql"
    t = sql.read_text(encoding="utf-8")
    if db not in t:
        t = t.rstrip("\n") + f"\n\nSELECT 'CREATE DATABASE {db}' \nWHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '{db}')\\gexec\n"
        print("patch init-dbs.sql")
        if not a.dry_run:
            sql.write_text(t, encoding="utf-8", newline="\n")
    env = REPO / "infra/docker/.env.example"
    t = env.read_text(encoding="utf-8")
    if f"{lower.upper()}_DB_NAME" not in t:
        t = re.sub(r"(\n[A-Z]+_DB_NAME=[^\n]*)(?!.*_DB_NAME=)", lambda m: m.group(1) + f"\n{lower.upper()}_DB_NAME={db}", t, count=1, flags=re.S)
        print("patch .env.example")
        if not a.dry_run:
            env.write_text(t, encoding="utf-8", newline="\n")

    # --- CI test loops -------------------------------------------------------------------------------------------
    ci = REPO / ".github/workflows/backend.yml"
    t = ci.read_text(encoding="utf-8")
    if f"OmniCore.Services.{a.name}.Domain.Tests" not in t:
        t = t.replace(" tests/OmniCore.Shared.Tests;",
                      f" tests/OmniCore.Services.{a.name}.Domain.Tests tests/OmniCore.Services.{a.name}.Application.Tests tests/OmniCore.Shared.Tests;", 1)
        t = re.sub(r"(tests/OmniCore\.Services\.\w+\.IntegrationTests)(; do)(?![\s\S]*IntegrationTests; do)",
                   rf"\1 tests/OmniCore.Services.{a.name}.IntegrationTests\2", t, count=1)
        print("patch backend.yml")
        if not a.dry_run:
            ci.write_text(t, encoding="utf-8", newline="\n")

    print(f"""
Still to do by hand (decisions, not boilerplate):
  1. infra/docker/docker-compose.services.yml  - add the service (copy user-api), env DATABASE__CONNECTIONSTRING -> {db}
       {lower}-api:
         <<: *service-base
         build: {{ context: ../.., dockerfile: backend/c#/OmniCore.Services.{a.name}/OmniCore.Services.{a.name}.Api/Dockerfile }}
         ports: ["${{{lower.upper()}_SERVICE_PORT:-{a.port}}}:8080"]
  2. infra/docker/nginx/nginx.conf             - add `location /api/v1/{ctx['__route__']}/ {{ ... }}` and /specs/{lower}.json (copy the users block)
  3. .env.example                              - {lower.upper()}_SERVICE_PORT and SWAGGER_URLS entry
  4. Generate the first migration (pwsh add-migration.ps1, or the CI generator branch) and commit it with the entities
  5. Write docs: service README, ADR if the design is risky, CLAUDE.md map, services-plan status
""")


if __name__ == "__main__":
    main()
