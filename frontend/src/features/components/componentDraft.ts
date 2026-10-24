import type { Component, ComponentInput } from '../../api/components'

/** Form state for one component; quantity stays a string while the user types. */
export type ComponentDraft = {
  name: string
  description: string
  quantity: string
}

export const emptyDraft: ComponentDraft = { name: '', description: '', quantity: '1' }

export function draftFrom(component: Component): ComponentDraft {
  return { name: component.name, description: component.description, quantity: String(component.quantity) }
}

/** An empty quantity is sent as 0 so the server reports it with its range message. */
export function toInput(draft: ComponentDraft): ComponentInput {
  return { name: draft.name, description: draft.description, quantity: Number(draft.quantity) || 0 }
}
