import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.2/firebase-app.js";
import { getAuth, connectAuthEmulator } from "https://www.gstatic.com/firebasejs/10.12.2/firebase-auth.js";

const firebaseConfig = window.firebaseConfig;

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

// Uncomment if using local emulator
// if (location.hostname === "localhost") {
//   connectAuthEmulator(auth, "http://localhost:9099");
// }

export { app, auth };
