import type { Component } from '../../api/components'

/** Shows how many copies a board gets, e.g. "LED green ×4", since the quantity comes with the component. */
export function componentLabel(component: Component): string {
  return `${component.name} ×${component.quantity}`
}
