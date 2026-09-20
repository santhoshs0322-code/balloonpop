package com.gamixtv.balloonpop.auth;

import android.app.Activity;
import android.os.CancellationSignal;
import androidx.annotation.NonNull;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.NoCredentialException;
import androidx.credentials.CredentialManagerCallback;
import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.unity3d.player.UnityPlayer;
import java.util.concurrent.Executor;

public final class NativeGoogleAuth {
    private static final String UNITY_OBJECT = "AuthManager";

    private NativeGoogleAuth() {}

    public static void signIn(String webClientId) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null || webClientId == null || webClientId.trim().isEmpty()) {
            sendError("Google sign-in is not configured");
            return;
        }

        activity.runOnUiThread(() -> {
            try {
                GetSignInWithGoogleOption option = new GetSignInWithGoogleOption.Builder(webClientId).build();
                GetCredentialRequest request = new GetCredentialRequest.Builder()
                    .addCredentialOption(option)
                    .build();
                CredentialManager manager = CredentialManager.Companion.create(activity);
                Executor mainExecutor = command -> activity.runOnUiThread(command);
                manager.getCredentialAsync(
                    activity,
                    request,
                    new CancellationSignal(),
                    mainExecutor,
                    new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                        @Override public void onResult(GetCredentialResponse response) {
                            handleCredential(response.getCredential());
                        }

                        @Override public void onError(@NonNull GetCredentialException error) {
                            if (error instanceof NoCredentialException) {
                                sendError("No Google account is available on this device");
                            } else {
                                sendError(error.getMessage() == null ? "Google sign-in was not completed" : error.getMessage());
                            }
                        }
                    }
                );
            } catch (Exception error) {
                sendError(error.getMessage() == null ? "Could not start Google sign-in" : error.getMessage());
            }
        });
    }

    public static void clearCredentialState() {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) return;
        activity.runOnUiThread(() -> {
            try {
                CredentialManager manager = CredentialManager.Companion.create(activity);
                Executor mainExecutor = command -> activity.runOnUiThread(command);
                manager.clearCredentialStateAsync(
                    new androidx.credentials.ClearCredentialStateRequest(),
                    new CancellationSignal(),
                    mainExecutor,
                    new CredentialManagerCallback<Void, androidx.credentials.exceptions.ClearCredentialException>() {
                        @Override public void onResult(Void ignored) {}
                        @Override public void onError(@NonNull androidx.credentials.exceptions.ClearCredentialException ignored) {}
                    }
                );
            } catch (Exception ignored) {}
        });
    }

    private static void handleCredential(Credential credential) {
        if (!(credential instanceof CustomCredential) ||
            !GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType())) {
            sendError("Google returned an unsupported credential");
            return;
        }
        try {
            GoogleIdTokenCredential googleCredential = GoogleIdTokenCredential.createFrom(credential.getData());
            UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnNativeGoogleToken", googleCredential.getIdToken());
        } catch (Exception error) {
            sendError("Google returned an invalid identity token");
        }
    }

    private static void sendError(String message) {
        UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnNativeGoogleError", message == null ? "Google sign-in failed" : message);
    }
}
