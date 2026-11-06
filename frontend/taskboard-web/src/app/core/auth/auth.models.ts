export interface User {
  id: string;
  email: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
}

export interface ApiProblem {
  status?: number;
  code?: string;
  errors?: Array<{ code: string; field?: string }>;
}
