import { redirect } from "next/navigation";

export default function Home() {
  redirect("/profile"); // proxy.ts sends signed-out visitors on to /login
}
