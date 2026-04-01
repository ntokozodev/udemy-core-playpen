import { apiConfig } from "@/services/apiConfig";
import { mockHttp } from "@/services/mockApi";

const API_BASE = "/admin/api";

function statusErrorMessage(status: number): string {
  if (status === 401 || status === 403) {
    return "You are not authorized to perform this action.";
  }

  if (status >= 500) {
    return "The server returned an error. Please try again.";
  }

  return "Request failed. Please try again.";
}

function extractMessageFromUnknown(value: unknown): string | null {
  if (!value || typeof value !== "object") {
    return null;
  }

  const payload = value as Record<string, unknown>;

  const message = payload.message;
  if (typeof message === "string" && message.trim().length > 0) {
    return message.trim();
  }

  const error = payload.error;
  if (typeof error === "string" && error.trim().length > 0) {
    return error.trim();
  }

  const title = payload.title;
  if (typeof title === "string" && title.trim().length > 0) {
    return title.trim();
  }

  return null;
}

function extractParseableTextMessage(text: string): string | null {
  const trimmed = text.trim();
  if (!trimmed) {
    return null;
  }

  if (trimmed.startsWith("<") || /<!doctype html/i.test(trimmed)) {
    return null;
  }

  try {
    const parsed = JSON.parse(trimmed) as unknown;
    return extractMessageFromUnknown(parsed);
  } catch {
    return trimmed;
  }
}

export async function http<T>(path: string, init?: RequestInit): Promise<T> {
  if (apiConfig.useMockData) {
    return mockHttp<T>(path, init);
  }

  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  const contentType = response.headers.get("content-type") ?? "";
  const isJsonResponse = contentType.includes("application/json");

  if (!response.ok) {
    if (isJsonResponse) {
      const errorBody = (await response.json().catch(() => null)) as unknown;
      throw new Error(extractMessageFromUnknown(errorBody) ?? statusErrorMessage(response.status));
    }

    const text = await response.text();
    throw new Error(extractParseableTextMessage(text) ?? statusErrorMessage(response.status));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  if (!isJsonResponse) {
    throw new Error("Unexpected response format from server. Expected JSON.");
  }

  return response.json() as Promise<T>;
}
