# Unity Game Simulation + Cloud Build

Upload Cloud Build build to Unity Game Simulation and run a new simulation at the end of a build.

## Setup

1. Open the project in Unity version 2019.1.12f1
2. Connect the project to a Unity Project via the `Services Window`
3. Enable [Unity Cloud Build](https://learn.unity.com/tutorial/unity-cloud-build) via the Services Window.
    * `Platform`: `Linux desktop 64-bit`
    * `Target label`: `Unity Game Simulation`
    * `Branch`: `master`
    * `Unity Version`: `Always Use Latest 2019.1`
    * `Auto-build`: `On` or `off`
4. Go to the Cloud Build dashboard for the project
5. Choose `Config` on the side nav
6. Expand `Advanced Options`
7. Choose `Edit Advanced Options`
8. Check `Yes, I'd like to enable Headless Build Mode`
9. For `Post-Export Method Name`, enter `Build.PostExport`
10. Choose `Save`
11. Expand `Environment Variables`
12. Choose `Edit Environment Variables`
13. Environment variable key `UNITY_PROJECT_ID`
14. Environment variable value, the Unity Project Id
15. `Add`
16. Environment variable key `UNITY_USERNAME`
17. Environment variable value, your Unity ID username
18. `Add`
19. Environment variable key `UNITY_PASSWORD`
20. Environment variable value, your Unity ID password
21. `Add`
22. `Save`
23. Choose `History` on the side nav
24. Chose `Build: Unity Game Simulation`