import { useEffect, useRef, useState, useCallback } from 'react';
import { Container, Text, Stack, Button, Alert, Loader, Group, Center, Select } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import type { CameraDevice } from 'html5-qrcode';
import { useNavigate } from 'react-router-dom';
import { notaService } from '../services/notaService';
import type { ImportQrCodeResult } from '../services/notaService';
import axios from 'axios';

const READER_ID = 'html5qr-code-reader';

export function ImportarNotaQrCodePage() {
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const scanningRef = useRef(false);
  const decodedRef = useRef(false);
  const navigate = useNavigate();

  const [cameras, setCameras] = useState<CameraDevice[]>([]);
  const [selectedCameraId, setSelectedCameraId] = useState<string | null>(null);
  const [scannedUrl, setScannedUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);
  const [result, setResult] = useState<ImportQrCodeResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [cameraStarting, setCameraStarting] = useState(true);

  const stopScanner = useCallback(async () => {
    try {
      if (scannerRef.current && scanningRef.current) {
        await scannerRef.current.stop();
        scanningRef.current = false;
      }
    } catch {
      // ignore – camera may already be stopped
    }
  }, []);

  /** Start (or restart) the scanner with the given cameraId or facingMode fallback */
  const startScanner = useCallback(async (cameraId?: string) => {
    // Make sure any previous instance is stopped before creating a new one
    await stopScanner();

    setCameraStarting(true);
    setError(null);
    decodedRef.current = false;

    try {
      scannerRef.current = new Html5Qrcode(READER_ID, {
        formatsToSupport: [Html5QrcodeSupportedFormats.QR_CODE],
        verbose: false,
      });

      const qrboxSize = Math.min(250, Math.floor(window.innerWidth * 0.7));

      // If we have a specific cameraId use it; otherwise default to rear camera
      const cameraConfig: string | { facingMode: string } =
        cameraId ?? { facingMode: 'environment' };

      await scannerRef.current.start(
        cameraConfig,
        { fps: 5, qrbox: { width: qrboxSize, height: qrboxSize } },
        (decodedText) => {
          if (decodedRef.current) return;
          decodedRef.current = true;
          setScannedUrl(decodedText);
          stopScanner();
        },
        () => { /* no match yet */ },
      );

      scanningRef.current = true;
    } catch (err) {
      console.error('Camera error:', err);
      const msg = err instanceof Error ? err.message : String(err);

      if (msg.includes('NotAllowedError') || msg.includes('Permission')) {
        setError('Permissão de câmera negada. Libere o acesso nas configurações do navegador.');
      } else if (msg.includes('NotFoundError') || msg.includes('Requested device not found')) {
        setError('Nenhuma câmera encontrada neste dispositivo.');
      } else {
        setError(`Não foi possível acessar a câmera: ${msg}`);
      }
    } finally {
      setCameraStarting(false);
    }
  }, [stopScanner]);

  // List cameras and auto-start with the best rear camera
  useEffect(() => {
    (async () => {
      try {
        const devices = await Html5Qrcode.getCameras();
        setCameras(devices);

        if (devices.length === 0) {
          setError('Nenhuma câmera encontrada neste dispositivo.');
          setCameraStarting(false);
          return;
        }

        // Try to pick a rear camera by label heuristic
        const rearCamera = devices.find((d) =>
          /back|rear|traseira|environment/i.test(d.label),
        );
        const chosen = rearCamera ?? devices[devices.length - 1]; // last is usually rear on mobile
        setSelectedCameraId(chosen.id);
        await startScanner(chosen.id);
      } catch {
        // If getCameras fails (permission not yet granted), start with facingMode
        await startScanner();
      }
    })();

    return () => { stopScanner(); };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /** User picks a different camera from the dropdown */
  const handleCameraChange = async (value: string | null) => {
    if (!value) return;
    setSelectedCameraId(value);
    setScannedUrl(null);
    setSent(false);
    await startScanner(value);
  };

  const handleSend = async () => {
    if (!scannedUrl) return;
    setLoading(true);
    setError(null);
    try {
      const data = await notaService.importarNotaPorQrCode(scannedUrl);
      setResult(data);
      setSent(true);
      notifications.show({
        title: 'Nota enviada',
        message: data.message || 'Nota enviada para processamento',
        color: 'green',
      });
    } catch (e: unknown) {
      let msg = 'Erro ao enviar a nota';
      if (axios.isAxiosError(e) && e.response?.data?.message) {
        msg = e.response.data.message;
      } else if (e instanceof Error) {
        msg = e.message;
      }
      setError(msg);
      notifications.show({ title: 'Erro', message: msg, color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  const handleScanAnother = async () => {
    setScannedUrl(null);
    setSent(false);
    setResult(null);
    setError(null);
    await startScanner(selectedCameraId ?? undefined);
  };

  return (
    <Container size="sm" py="lg">
      <Stack gap="md">
        <Text size="lg" fw={600}>
          Aponte a câmera para o QR Code da nota fiscal
        </Text>

        {/* Camera selector – only shown when device has more than 1 camera */}
        {cameras.length > 1 && (
          <Select
            label="Câmera"
            placeholder="Selecione a câmera"
            value={selectedCameraId}
            onChange={handleCameraChange}
            data={cameras.map((c, i) => ({
              value: c.id,
              label: c.label || `Câmera ${i + 1}`,
            }))}
          />
        )}

        {/* Camera viewport – always in the DOM so Html5Qrcode can attach */}
        <div
          id={READER_ID}
          style={{
            width: '100%',
            minHeight: 300,
            display: scannedUrl ? 'none' : 'block',
            borderRadius: 8,
            overflow: 'hidden',
          }}
        />

        {cameraStarting && !scannedUrl && (
          <Center>
            <Loader size="sm" mr="xs" /> <Text size="sm">Iniciando câmera…</Text>
          </Center>
        )}

        {scannedUrl && (
          <Alert color="blue" title="QR Code detectado">
            {scannedUrl}
          </Alert>
        )}

        {error && (
          <Alert color="red" title="Erro">
            {error}
          </Alert>
        )}

        <Group justify="flex-end">
          {scannedUrl && !sent && !loading && (
            <Button onClick={handleSend}>Importar esta nota</Button>
          )}

          {loading && <Loader size="sm" />}

          {sent && result && (
            <Stack gap="xs" style={{ width: '100%' }}>
              <Alert color="green" title="Nota importada com sucesso!">
                {result.empresa && <Text size="sm">Empresa: <strong>{result.empresa}</strong></Text>}
                {result.message && <Text size="sm">{result.message}</Text>}
              </Alert>
              <Group justify="flex-end">
                {result.listaId && (
                  <Button variant="light" onClick={() => navigate(`/listas/${result.listaId}`)}>
                    Ver lista
                  </Button>
                )}
                <Button onClick={handleScanAnother}>Ler outra nota</Button>
              </Group>
            </Stack>
          )}

          {sent && !result && (
            <Button onClick={handleScanAnother}>Ler outra nota</Button>
          )}

          {error && !scannedUrl && (
            <Button variant="outline" onClick={() => startScanner(selectedCameraId ?? undefined)}>
              Tentar novamente
            </Button>
          )}
        </Group>
      </Stack>
    </Container>
  );
}
