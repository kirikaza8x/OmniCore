import { LogoutButton } from "@/features/auth/components/LogoutButton";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="mx-auto max-w-3xl px-4 py-6">
      <nav className="mb-8 flex items-center justify-between border-b border-zinc-200 pb-4 dark:border-zinc-800">
        <span className="font-semibold">OmniCore</span>
        <LogoutButton />
      </nav>
      {children}
    </div>
  );
}
