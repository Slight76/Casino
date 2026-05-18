import { useEffect, useRef, useState } from "react"
import * as signalR from "@microsoft/signalr"
import { usePlayer } from "../context/PlayerContext"

const BASE = "http://localhost:5000"

/**
 * Builds, starts, and returns a SignalR HubConnection.
 * Automatically reconnects and cleans up on unmount.
 */
export function useHubConnection(hubPath: string) {
  const { player } = usePlayer()
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const connRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}${hubPath}`, {
        accessTokenFactory: () => player?.token ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connRef.current = conn

    conn
      .start()
      .then(() => setConnection(conn))
      .catch((err) => console.error(`[SignalR] Failed to connect to ${hubPath}:`, err))

    return () => {
      conn.stop().catch(() => {})
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hubPath, player?.token])

  return connection
}
