type FieldErrorProps = {
  messages?: string[]
}

/** Shows the server's validation messages for one field. */
export function FieldError({ messages }: FieldErrorProps) {
  if (!messages?.length) return null
  return <p className="field-error">{messages.join(' ')}</p>
}
