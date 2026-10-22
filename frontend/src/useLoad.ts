import { useCallback, useEffect, useState } from 'react';
import { ApiError } from './api/client';

/** Bir isteği yükler; yükleniyor / hata / yeniden yükle durumunu verir. */
export function useLoad<T>(fetcher: () => Promise<T>, deps: unknown[]) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setData(await fetcher());
    } catch (err) {
      setData(null);
      setError(err instanceof ApiError ? err.message : 'Veri yüklenemedi.');
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    load();
  }, [load]);

  return { data, loading, error, reload: load };
}
