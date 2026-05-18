import { useState, type FormEvent } from "react"
import { useNavigate } from "react-router-dom"
import { usePlayer } from "../context/PlayerContext"

export default function NamePrompt() {
  const { signIn } = usePlayer()
  const navigate = useNavigate()
  const [name, setName] = useState("")
  const [error, setError] = useState("")
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) { setError("Please enter a name."); return }
    try {
      setLoading(true)
      setError("")
      await signIn(trimmed)
      navigate("/lobby")
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong.")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ maxWidth: 400, margin: "4rem auto" }}>
      <div className="card">
        <h1>🎰 Casino</h1>
        <p style={{ marginBottom: "1.5rem", opacity: 0.75 }}>Enter your display name to get started.</p>
        <form onSubmit={handleSubmit}>
          <input
            type="text"
            placeholder="Your name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={loading}
            autoFocus
          />
          {error && <p style={{ color: "#f87171", marginBottom: "0.75rem" }}>{error}</p>}
          <button type="submit" className="btn-primary" disabled={loading} style={{ width: "100%" }}>
            {loading ? "Joining…" : "Enter Casino"}
          </button>
        </form>
      </div>
    </div>
  )
}
