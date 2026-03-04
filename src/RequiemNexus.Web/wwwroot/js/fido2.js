// Convert ArrayBuffer to Base64Url string
function bufferToBase64url(buffer) {
    const bytes = new Uint8Array(buffer);
    let str = "";
    for (const charCode of bytes) {
        str += String.fromCharCode(charCode);
    }
    const base64String = btoa(str);
    return base64String.replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");
}

// Convert Base64Url string to ArrayBuffer
function base64urlToBuffer(base64url) {
    const padding = "==".slice(0, (4 - base64url.length % 4) % 4);
    const base64 = base64url.replace(/-/g, "+").replace(/_/g, "/") + padding;
    const str = atob(base64);
    const buffer = new ArrayBuffer(str.length);
    const byteView = new Uint8Array(buffer);
    for (let i = 0; i < str.length; i++) {
        byteView[i] = str.charCodeAt(i);
    }
    return buffer;
}

window.fido2Interop = {
    makeCredential: async function (makeCredentialOptionsJson) {
        try {
            const makeCredentialOptions = JSON.parse(makeCredentialOptionsJson);

            // Decode challenge
            makeCredentialOptions.challenge = base64urlToBuffer(makeCredentialOptions.challenge);

            // Decode user.id
            makeCredentialOptions.user.id = base64urlToBuffer(makeCredentialOptions.user.id);

            // Decode excludeCredentials
            if (makeCredentialOptions.excludeCredentials) {
                for (let cred of makeCredentialOptions.excludeCredentials) {
                    cred.id = base64urlToBuffer(cred.id);
                }
            }

            const credential = await navigator.credentials.create({
                publicKey: makeCredentialOptions
            });

            // Convert back to base64url for C#
            const rawId = bufferToBase64url(credential.rawId);
            const clientDataJSON = bufferToBase64url(credential.response.clientDataJSON);
            const attestationObject = bufferToBase64url(credential.response.attestationObject);

            return JSON.stringify({
                id: credential.id,
                rawId: rawId,
                type: credential.type,
                extensions: credential.getClientExtensionResults(),
                response: {
                    attestationObject: attestationObject,
                    clientDataJSON: clientDataJSON
                }
            });
        } catch (e) {
            console.error(e);
            return JSON.stringify({ error: e.message });
        }
    },

    getAssertion: async function (makeAssertionOptionsJson) {
        try {
            const makeAssertionOptions = JSON.parse(makeAssertionOptionsJson);

            // Decode challenge
            makeAssertionOptions.challenge = base64urlToBuffer(makeAssertionOptions.challenge);

            // Decode allowCredentials
            if (makeAssertionOptions.allowCredentials) {
                for (let cred of makeAssertionOptions.allowCredentials) {
                    cred.id = base64urlToBuffer(cred.id);
                }
            }

            const assertion = await navigator.credentials.get({
                publicKey: makeAssertionOptions
            });

            const rawId = bufferToBase64url(assertion.rawId);
            const clientDataJSON = bufferToBase64url(assertion.response.clientDataJSON);
            const authenticatorData = bufferToBase64url(assertion.response.authenticatorData);
            const signature = bufferToBase64url(assertion.response.signature);
            const userHandle = assertion.response.userHandle ? bufferToBase64url(assertion.response.userHandle) : null;

            return JSON.stringify({
                id: assertion.id,
                rawId: rawId,
                type: assertion.type,
                extensions: assertion.getClientExtensionResults(),
                response: {
                    authenticatorData: authenticatorData,
                    signature: signature,
                    clientDataJSON: clientDataJSON,
                    userHandle: userHandle
                }
            });
        } catch (e) {
            console.error(e);
            return JSON.stringify({ error: e.message });
        }
    }
};
