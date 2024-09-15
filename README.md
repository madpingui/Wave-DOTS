# Wave-DOTS

What I'm doing in this project is instantiating 40,000 cubes side by side to form a layer. The system detects when I click, and it generates a wave. Each entity is informed of the wave and modifies its vertical position depending on the distance from the wave's origin. Several factors can be adjusted, such as amplitude, frequency, speed, and damping over time.

Currently, multiple waves can be generated, so each entity must take into account the sum or destruction of wave amplitudes. Additionally, I added a color system that makes the entity brighter at the wave's peak and darker at the bottom (the colors can be customized).

In my tests, I managed to instantiate over 60,000 entities with a stable frame rate above 60 FPS, and in this test I'm showing, it easily surpasses 144 FPS (limited by my screen's refresh rate).

https://github.com/user-attachments/assets/4893b44c-1de5-484b-9f52-9de11d70b89b
