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

async function send(method: string, path: string, body?: unknown): Promise<Response> {
  const headers: Record<string, string> = {};
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  let payload: BodyInit | undefined;
  if (body instanceof FormData) {
    payload = body; // tarayıcı sınır (boundary) bilgisini kendisi ekler
  } else if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
    payload = JSON.stringify(body);
  }

  try {
    return await fetch(path, { method, headers, body: payload });
  } catch {
    throw new ApiError(0, 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.');
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const token = getToken();
  const res = await send(method, path, body);

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

/** Yetkili isteğle dosya indirir ve kullanıcıya kaydettirir. */
async function download(path: string, fallbackName: string): Promise<void> {
  const res = await send('GET', path);
  if (!res.ok) {
    const data = safeParse(await res.text());
    throw new ApiError(res.status, (data?.detail as string) || 'Dosya indirilemedi.');
  }
  const disposition = res.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const name = match ? decodeURIComponent(match[1]) : fallbackName;
  const url = URL.createObjectURL(await res.blob());
  const link = document.createElement('a');
  link.href = url;
  link.download = name;
  link.click();
  URL.revokeObjectURL(url);
}

export const api = {
  download,
  upload: <T>(path: string, form: FormData) => request<T>('POST', path, form),
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body ?? {}),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body ?? {}),
  del: (path: string) => request<void>('DELETE', path),
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
