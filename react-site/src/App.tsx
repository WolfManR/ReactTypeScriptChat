import { useState, useEffect } from 'react'
import './App.css'

interface Weather {
  date: string
  temperatureC: number
  summary: string
  temperatureF: number
}

function App() {
  const [weather, setWeather] = useState<Array<Weather>>()

  useEffect(() => {
    fetch('http://localhost:5271/weatherforecast')
      .then((response) => response.json())
      .then((json) => setWeather(json as Array<Weather>))
  }, [])

  if (!weather) return <div className="App">Loading...</div>

  return (
    <div className="App">
      {weather.map((w: Weather, i: number) => (
        <p key={i}>
          {new Date(w.date).toLocaleTimeString()} {w.temperatureC} {w.summary}{' '}
          {w.temperatureF}
        </p>
      ))}
    </div>
  )
}

export default App
