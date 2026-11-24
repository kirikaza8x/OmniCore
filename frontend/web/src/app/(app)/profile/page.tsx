import { EmailVerificationStatus } from "@/features/auth/components/EmailVerificationStatus";
import { ProfilePage } from "@/features/profile/components/ProfilePage";

// `app` composes features: the e-mail verification action belongs to auth, the page to profile.
export default function Profile() {
  return (
    <>
      <ProfilePage />
      <section className="mx-auto max-w-2xl px-4 pb-10">
        <h2 className="mb-2 text-sm font-medium">E-mail verification</h2>
        <EmailVerificationStatus />
      </section>
    </>
  );
}
