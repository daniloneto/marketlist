import { useEffect, useRef, useState, useCallback } from 'react';
import { Container, Text, Stack, Button, Alert, Loader, Group, Center } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { Html5Qrcode } from 'html5-qrcode';
import { notaService } from '../services/notaService';

const READER_ID = 'html5qr-code-reader';

export function ImportarNotaQrCodePage() {
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const scanningRef = useRef(false);
  const [scannedUrl, setScannedUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);
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

  const startScanner = useCallback(async () => {
    setCameraStarting(true);
    setError(null);

    try {
      // Create a fresh instance every time
      scannerRef.current = new Html5Qrcode(READER_ID);

      // Responsive qrbox – 70 % of the container, max 250 px
      const qrboxSize = Math.min(250, Math.floor(window.innerWidth * 0.7));

      await scannerRef.current.start(
        { facingMode: 'environment' },          // rear camera
        { fps: 10, qrbox: { width: qrboxSize, height: qrboxSize } },
        (decodedText) => {
          // Success – stop camera and surface the URL
          setScannedUrl(decodedText);
          stopScanner();
        },
        () => {
          // Scan frame – no match yet, nothing to do
        },
      );

      scanningRef.current = true;
    } catch (err) {
      console.error('Camera error:', err);
      const msg =
        err instanceof Error ? err.message : String(err);

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

  // Start on mount, cleanup on unmount
  useEffect(() => {
    startScanner();

    return () => {
      stopScanner();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSend = async () => {
    if (!scannedUrl) return;
    setLoading(true);
    setError(null);
    try {
      await notaService.importarNotaPorQrCode(scannedUrl);
      setSent(true);
      notifications.show({ title: 'Nota enviada', message: 'Nota enviada para processamento', color: 'green' });
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Erro ao enviar a nota';
      setError(msg);
      notifications.show({ title: 'Erro', message: msg, color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  const handleScanAnother = async () => {
    setScannedUrl(null);
    setSent(false);
    setError(null);
    await startScanner();
  };

  return (
    <Container size="sm" py="lg">
      <Stack gap="md">
        <Text size="lg" fw={600}>
          Aponte a câmera para o QR Code da nota fiscal
        </Text>

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

          {sent && (
            <Button onClick={handleScanAnother}>Ler outra nota</Button>
          )}

          {error && !scannedUrl && (
            <Button variant="outline" onClick={() => startScanner()}>
              Tentar novamente
            </Button>
          )}
        </Group>
      </Stack>
    </Container>
  );
}
