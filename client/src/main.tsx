import React from "react"
import ReactDOM from "react-dom/client"
import { RouterProvider } from "react-router-dom"
import { PlayerProvider } from "./context/PlayerContext"
import { router } from "./router"
import "./App.css"

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <PlayerProvider>
      <RouterProvider router={router} />
    </PlayerProvider>
  </React.StrictMode>
)
