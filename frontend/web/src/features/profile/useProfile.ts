"use client";

import { useCallback, useEffect, useState } from "react";
import { profileApi } from "./api";
import type { UserProfile } from "./types";

type State = { profile?: UserProfile; loading: boolean; error?: Error };

/** Loads the signed-in user's profile; `reload` fetches it again after a change (the old data stays visible meanwhile). */
export function useProfile() {
  const [state, setState] = useState<State>({ loading: true });
  const [tick, setTick] = useState(0);

  useEffect(() => {
    let live = true;
    profileApi
      .get()
      .then((profile) => live && setState({ profile, loading: false }))
      .catch((error: Error) => live && setState({ loading: false, error }));
    return () => {
      live = false;
    };
  }, [tick]);

  const reload = useCallback(() => setTick((t) => t + 1), []);
  return { ...state, reload };
}
