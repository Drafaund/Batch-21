interface ErrorBannerProps {
  message: string
  onDismiss: () => void
}

export function ErrorBanner({ message, onDismiss }: ErrorBannerProps) {
  return (
    <div role="alert" className="error-banner">
      <span>{message}</span>
      <button type="button" onClick={onDismiss} aria-label="Dismiss the message">
        ×
      </button>
    </div>
  )
}
