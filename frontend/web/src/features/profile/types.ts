// Shapes of the User service responses (backend: User.Application/DTOs/ProfileDtos.cs).
export type UserSettings = { theme: string; language: string; emailNotificationsEnabled: boolean; pushNotificationsEnabled: boolean };

export type UserAddress = {
  id: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  state: string;
  country: string;
  zipCode: string;
  isPrimary: boolean;
};

export type UserProfile = {
  id: string;
  username: string;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  phoneNumber?: string | null;
  isActive: boolean;
  settings: UserSettings;
  addresses: UserAddress[];
};
