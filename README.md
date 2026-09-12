# AR Marker Pizza Catch — MAMN60

A small marker-based AR game built with Unity, AR Foundation, and ARKit/ARCore image tracking.

## Concept

A physical pizza marker acts as the player's tray/controller. The iPad is mounted so its rear camera can see the play area while the player watches the screen.

Ingredients and bombs fall in AR. The player moves the physical marker left and right to catch ingredients and avoid bombs. After collecting six ingredients, the falling phase stops and the collected ingredients are assembled into a pizza, followed by a short baking/oven animation.

## Core Game Loop

1. Detect the pizza marker with image tracking.
2. Spawn/anchor the virtual pizza tray to the tracked marker.
3. Spawn falling ingredients and bombs above the play area.
4. Move the physical marker left/right to catch or avoid falling objects.
5. Store each caught ingredient.
6. When six ingredients have been collected, stop spawning.
7. Build the final pizza from the collected ingredient set.
8. Play the baking/oven animation and reveal the finished pizza.

## Planned Tech Stack

- Unity
- AR Foundation
- ARKit XR Plugin (iPad/iPhone)
- ARCore XR Plugin (optional Android support)
- XR Reference Image Library
- AR Tracked Image Manager

## MVP Milestones

- [ ] Marker is detected reliably on iPad
- [ ] Virtual tray follows the marker position and rotation
- [ ] Falling object spawner works
- [ ] Ingredients can be caught
- [ ] Bomb collision is detected
- [ ] Six caught ingredients end the collection phase
- [ ] Final pizza uses the caught ingredient combination
- [ ] Baking animation plays

## Physical Setup

- iPad mounted with rear camera facing the table/play area
- A3-sized play mat as a visual movement boundary
- Separate printed pizza marker mounted on card/paper plate
- Player moves the marker primarily left/right inside the play area

## Course

MAMN60 — AR marker demo
