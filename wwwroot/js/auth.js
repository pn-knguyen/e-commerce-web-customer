import { auth } from "./firebase-init.js";
import { 
  createUserWithEmailAndPassword, 
  signInWithEmailAndPassword, 
  GoogleAuthProvider, 
  FacebookAuthProvider,
  signInWithPopup, 
  RecaptchaVerifier, 
  signInWithPhoneNumber,
  signOut,
  onAuthStateChanged,
  sendSignInLinkToEmail,
  isSignInWithEmailLink,
  signInWithEmailLink,
  sendPasswordResetEmail
} from "https://www.gstatic.com/firebasejs/10.12.2/firebase-auth.js";

// --- Observe Auth State ---
onAuthStateChanged(auth, (user) => {
  if (user) {
    console.log("User signed in:", user.uid);
    // You can update UI here, or send the token to your backend
    // user.getIdToken().then(token => console.log(token));
  } else {
    console.log("User signed out");
  }
});

// --- Auth Methods ---

export function signUpWithEmail(email, password) {
  return createUserWithEmailAndPassword(auth, email, password);
}

export function signInWithEmail(email, password) {
  return signInWithEmailAndPassword(auth, email, password);
}

export function logOut() {
  return signOut(auth);
}

// --- Google Sign In ---
const googleProvider = new GoogleAuthProvider();

export function signInWithGoogle() {
  return signInWithPopup(auth, googleProvider)
    .then((result) => {
      const user = result.user;
      console.log("Google user signed in:", user.uid);
      return result;
    });
}

// --- Facebook Sign In ---
const facebookProvider = new FacebookAuthProvider();
// Yêu cầu quyền truy cập email một cách rõ ràng
facebookProvider.addScope('email');
// Ép Facebook hỏi lại quyền nếu trước đó người dùng đã từ chối cấp email
facebookProvider.setCustomParameters({
  'auth_type': 'rerequest'
});

export function signInWithFacebook() {
  return signInWithPopup(auth, facebookProvider)
    .then((result) => {
      const user = result.user;
      console.log("Facebook user signed in:", user.uid);
      
      // Xử lý fallback nếu Facebook kiên quyết không trả về email
      if (!user.email) {
        console.warn("Facebook didn't provide an email. Using fallback email.");
        // Gán một email tạm thời cho user object (lưu ý Firebase User object là readonly ở một số thuộc tính, 
        // nhưng ta có thể pass data qua extraData)
        result.fallbackEmail = `${user.uid}@techstore.local`;
      }
      return result;
    });
}

// --- Phone Auth ---
// Initializes ReCaptcha for Phone Auth
export function setupRecaptcha(containerId) {
  window.recaptchaVerifier = new RecaptchaVerifier(auth, containerId, {
    'size': 'invisible',
    'callback': (response) => {
      // reCAPTCHA solved, allow signInWithPhoneNumber.
      console.log("Recaptcha solved");
    }
  });
}

export function requestPhoneOTP(phoneNumber) {
  const appVerifier = window.recaptchaVerifier;
  return signInWithPhoneNumber(auth, phoneNumber, appVerifier)
    .then((confirmationResult) => {
      // SMS sent. Prompt user to type the code from the message, then sign in
      window.confirmationResult = confirmationResult;
      console.log("SMS sent to", phoneNumber);
      return confirmationResult;
    });
}

export function verifyPhoneOTP(code) {
  if (!window.confirmationResult) return Promise.reject("No OTP requested");
  return window.confirmationResult.confirm(code).then((result) => {
    // User signed in successfully.
    const user = result.user;
    console.log("Phone user signed in:", user.uid);
    return result;
  });
}

// --- Email Link Auth (Passwordless) ---

export function sendEmailLink(email, url) {
  const actionCodeSettings = {
    url: url,
    handleCodeInApp: true
  };
  return sendSignInLinkToEmail(auth, email, actionCodeSettings)
    .then(() => {
      window.localStorage.setItem('emailForSignIn', email);
    });
}

export function checkIsSignInWithEmailLink(url) {
  return isSignInWithEmailLink(auth, url);
}

export function completeEmailLinkSignIn(url, emailFallback = null) {
  if (isSignInWithEmailLink(auth, url)) {
    let email = window.localStorage.getItem('emailForSignIn');
    if (!email && emailFallback) {
      email = emailFallback;
    }
    
    if (!email) {
      return Promise.reject(new Error("EMAIL_REQUIRED"));
    }

    return signInWithEmailLink(auth, email, url)
      .then((result) => {
        window.localStorage.removeItem('emailForSignIn');
        return result;
      });
  }
  return Promise.resolve(null);
}

// --- Sync with ASP.NET Core Backend ---
export function syncWithBackend(user, returnUrl, extraData = {}) {
  // Lấy ID Token được mã hóa từ Firebase (cực kỳ bảo mật)
  return user.getIdToken(true).then(idToken => {
    return fetch('/Account/FirebaseSync', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        idToken: idToken,
        returnUrl: returnUrl,
        displayName: extraData.displayName,
        phoneNumber: extraData.phoneNumber,
        sessionId: extraData.sessionId
      })
    })
    .then(res => res.json())
    .then(data => {
      if (data.success) {
        window.location.href = data.returnUrl;
      } else {
        console.error("Backend sync failed:", data.message);
        const err = new Error(data.message);
        err.isBackend = true; // Cờ đánh dấu đây là lỗi do Backend, không phải lỗi Firebase
        throw err;
      }
    });
  }).catch(err => {
    console.error("Lỗi xác thực Token:", err);
    throw err;
  });
}

export function sendPasswordReset(email, continueUrl = null) {
  let actionCodeSettings = null;
  if (continueUrl) {
    actionCodeSettings = {
      url: continueUrl,
      handleCodeInApp: false
    };
  }
  
  return sendPasswordResetEmail(auth, email, actionCodeSettings)
    .then(() => {
      return true;
    })
    .catch((error) => {
      console.error("Lỗi gửi email khôi phục:", error);
      throw error;
    });
}

// Expose to window for easy access in views (if needed outside modules)
window.firebaseAuthHelpers = {
  signUpWithEmail,
  signInWithEmail,
  logOut,
  signInWithGoogle,
  signInWithFacebook,
  setupRecaptcha,
  requestPhoneOTP,
  verifyPhoneOTP,
  sendEmailLink,
  checkIsSignInWithEmailLink,
  completeEmailLinkSignIn,
  syncWithBackend,
  sendPasswordReset
};
