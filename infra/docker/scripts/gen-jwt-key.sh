#!/usr/bin/env bash
# Prints a THROWAWAY RSA key as a one-line JWT_PRIVATE_KEY_PEM entry for infra/docker/.env.
# Use it to try the prod override or the RS256 path locally. Never commit the output.
# For a real deployment generate and store the key yourself (see .env.example).
set -euo pipefail
key="$(openssl genrsa 2048 2>/dev/null | awk 'NF {printf "%s\\n", $0}')"
printf 'JWT_PRIVATE_KEY_PEM=%s\n' "$key"
