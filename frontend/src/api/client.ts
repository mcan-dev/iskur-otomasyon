const TOKEN_KEY = 'iskur.token';

export function getToken(): string | null {
  try {
    return sessionStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setToken(token: string | null) {
  try {
    if (token) sessionStorage.setItem(TOKEN_KEY, token);
    else sessionStorage.removeItem(TOKEN_KEY);
  } catch {
    // tarayıcı depolamayı engelliyorsa oturum yalnızca sayfa açıkken sürer
  }
}

/** Arka yüzün tek biçimli hata yanıtı (ProblemDetails). */
export class ApiError extends Error {
  status: number;
  fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {};
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let res: Response;
  try {
    res = await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  } catch {
    throw new ApiError(0, 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.');
  }

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  const data = text ? safeParse(text) : null;

  if (!res.ok) {
    if (res.status === 401 && token) onUnauthorized?.();
    const detail = (data?.detail as string) || (data?.title as string) || 'İşlem tamamlanamadı.';
    throw new ApiError(res.status, detail, (data?.errors as Record<string, string[]>) ?? {});
  }
  return data as T;
}

function safeParse(text: string): Record<string, unknown> | null {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body ?? {}),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body ?? {}),
};

/** Alan hatalarını tek satırlık okunur metne çevirir. */
export function firstError(err: unknown, field?: string): string | undefined {
  if (!(err instanceof ApiError)) return undefined;
  if (field) {
    const key = Object.keys(err.fieldErrors).find((k) => k.toLowerCase() === field.toLowerCase());
    return key ? err.fieldErrors[key][0] : undefined;
  }
  return err.message;
}
