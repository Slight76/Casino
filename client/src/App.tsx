import { Outlet } from "react-router-dom"
import { usePlayer } from "./context/PlayerContext"
import { Link } from "react-router-dom"

export default function App() {
  const { player, signOut } = usePlayer()

  return (
    <>
      <div className="status-bar">
        <nav>
          <Link to="/lobby">Lobby</Link>
          <Link to="/blackjack">Blackjack</Link>
          <Link to="/poker">Poker</Link>
          <Link to="/roulette">Roulette</Link>
        </nav>
        {player && (
          <div style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
            <span>👤 {player.name}</span>
            <button className="btn-danger" onClick={signOut}>Sign Off</button>
          </div>
        )}
      </div>
      <div className="container">
        <Outlet />
      </div>
    </>
  )
}
