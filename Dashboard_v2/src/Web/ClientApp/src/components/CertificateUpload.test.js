import React from 'react';
import { act } from 'react-dom/test-utils';
import { createRoot } from 'react-dom/client';
import CertificateUpload, { CertificateViewButton } from './CertificateUpload';

// ── helpers ────────────────────────────────────────────────────────────────

function makeFetchOk(jsonValue) {
  return jest.fn().mockResolvedValueOnce({
    ok: true,
    json: () => Promise.resolve(jsonValue),
    text: () => Promise.resolve(JSON.stringify(jsonValue)),
  });
}

function makeFetchError(status = 500) {
  return jest.fn().mockResolvedValueOnce({ ok: false, status });
}

let container;
let root;

beforeEach(() => {
  container = document.createElement('div');
  document.body.appendChild(container);
  root = createRoot(container);
  window.open = jest.fn();
});

afterEach(async () => {
  await act(async () => root.unmount());
  container.remove();
  jest.restoreAllMocks();
});

// ── CertificateViewButton ──────────────────────────────────────────────────

describe('CertificateViewButton', () => {
  test('renders nothing when fileId is null', () => {
    act(() => {
      root.render(<CertificateViewButton fileId={null} />);
    });
    expect(container.firstChild).toBeNull();
  });

  test('renders a button when fileId is provided', async () => {
    global.fetch = makeFetchOk('https://minio/file.pdf');
    await act(async () => {
      root.render(<CertificateViewButton fileId={42} />);
    });
    const button = container.querySelector('button');
    expect(button).not.toBeNull();
  });

  test('opens the plain URL from JSON-parsed presigned response', async () => {
    const presignedUrl = 'https://minio:9000/bucket/evidence.pdf?X-Amz-Signature=abc123';
    global.fetch = makeFetchOk(presignedUrl);

    await act(async () => {
      root.render(<CertificateViewButton fileId={42} />);
    });

    const button = container.querySelector('button');
    await act(async () => {
      button.click();
    });

    expect(global.fetch).toHaveBeenCalledWith(
      '/api/FileStorage/42/url',
      { credentials: 'include' },
    );
    // window.open must receive the plain URL, NOT the JSON-quoted string
    expect(window.open).toHaveBeenCalledWith(presignedUrl, '_blank', 'noopener,noreferrer');
    expect(window.open).not.toHaveBeenCalledWith(
      `"${presignedUrl}"`,
      expect.anything(),
      expect.anything(),
    );
  });

  test('does not open a window when the request fails', async () => {
    global.fetch = makeFetchError(403);

    await act(async () => {
      root.render(<CertificateViewButton fileId={7} />);
    });

    const button = container.querySelector('button');
    await act(async () => {
      button.click();
    });

    expect(window.open).not.toHaveBeenCalled();
    // An error message should appear
    const errorSpan = container.querySelector('.text-danger');
    expect(errorSpan).not.toBeNull();
  });

  test('is disabled while loading and re-enabled after', async () => {
    let resolveJson;
    const jsonPromise = new Promise(res => { resolveJson = res; });
    global.fetch = jest.fn().mockResolvedValueOnce({
      ok: true,
      json: () => jsonPromise,
    });

    await act(async () => {
      root.render(<CertificateViewButton fileId={5} />);
    });

    const button = container.querySelector('button');
    expect(button.disabled).toBe(false);

    // Start the click without awaiting, so we can inspect mid-flight state
    const clickPromise = act(async () => { button.click(); });

    // Resolve the fetch
    await act(async () => { resolveJson('https://minio/file.pdf'); });
    await clickPromise;

    expect(button.disabled).toBe(false);
  });
});

// ── CertificateUpload (handleView path) ───────────────────────────────────

describe('CertificateUpload – handleView', () => {
  const noop = () => {};

  test('opens the plain URL when "Ver / Descargar" is clicked', async () => {
    const presignedUrl = 'https://minio:9000/bucket/award.pdf?sig=xyz';
    global.fetch = makeFetchOk(presignedUrl);

    await act(async () => {
      root.render(
        <CertificateUpload
          fileId={10}
          onFileIdChange={noop}
          canManage={false}
          canView={true}
        />,
      );
    });

    const button = container.querySelector('button');
    expect(button).not.toBeNull();

    await act(async () => {
      button.click();
    });

    expect(global.fetch).toHaveBeenCalledWith(
      '/api/FileStorage/10/url',
      { credentials: 'include' },
    );
    expect(window.open).toHaveBeenCalledWith(presignedUrl, '_blank', 'noopener,noreferrer');
  });

  test('shows error alert when the request fails', async () => {
    global.fetch = makeFetchError(404);

    await act(async () => {
      root.render(
        <CertificateUpload
          fileId={10}
          onFileIdChange={noop}
          canManage={false}
          canView={true}
        />,
      );
    });

    const button = container.querySelector('button');
    await act(async () => {
      button.click();
    });

    expect(window.open).not.toHaveBeenCalled();
    const alert = container.querySelector('.alert');
    expect(alert).not.toBeNull();
  });

  test('shows "Sin certificado" when fileId is null and canManage is false', async () => {
    await act(async () => {
      root.render(
        <CertificateUpload
          fileId={null}
          onFileIdChange={noop}
          canManage={false}
          canView={true}
        />,
      );
    });
    expect(container.textContent).toMatch(/Sin certificado/);
  });

  test('shows file input when fileId is null and canManage is true', async () => {
    await act(async () => {
      root.render(
        <CertificateUpload
          fileId={null}
          onFileIdChange={noop}
          canManage={true}
          canView={false}
        />,
      );
    });
    expect(container.textContent).toMatch(/Adjuntar certificado/);
    const fileInput = container.querySelector('input[type="file"]');
    expect(fileInput).not.toBeNull();
  });
});
