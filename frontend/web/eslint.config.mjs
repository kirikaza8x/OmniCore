import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

// Layer rules (same idea as the backend): app -> features -> shared. A lower layer never imports a higher one,
// and a feature never imports another feature.
const boundary = (message, patterns) => ({
  "no-restricted-imports": ["error", { patterns: patterns.map((group) => ({ group: [group], message })) }],
});

const config = [
  ...nextVitals,
  ...nextTs,
  { ignores: [".next/**", "node_modules/**", "next-env.d.ts"] },
  {
    files: ["src/shared/**/*.{ts,tsx}"],
    rules: boundary("shared must not import features or app", ["@/features/*", "@/app/*"]),
  },
  {
    files: ["src/features/auth/**/*.{ts,tsx}"],
    rules: boundary("a feature must not import another feature or app", ["@/features/profile/*", "@/app/*"]),
  },
  {
    files: ["src/features/profile/**/*.{ts,tsx}"],
    rules: boundary("a feature must not import another feature or app", ["@/features/auth/*", "@/app/*"]),
  },
];

export default config;
