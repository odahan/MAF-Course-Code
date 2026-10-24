const element = id => document.getElementById(id);
const backend = element("settings").dataset.backend;
const session = element("sessionId");
session.value = sessionStorage.getItem("maf-session") ?? crypto.randomUUID().replaceAll("-", "");
let controller;

function appendMessage(role, text) {
    const article = document.createElement("article");
    article.className = "message";
    const heading = document.createElement("strong");
    heading.textContent = role;
    const content = document.createElement("div");
    content.className = "message-texte";
    content.textContent = text;
    article.append(heading, content);
    element("messages").append(article);
    return content;
}

function busy(value) {
    for (const id of ["stream", "complete", "new", "prompt", "sessionId"]) element(id).disabled = value;
    element("cancel").disabled = !value;
}

async function send(streaming) {
    const prompt = element("prompt").value;
    if (!prompt.trim()) return;
    if (!/^[a-f0-9]{32}$/i.test(session.value)) {
        element("status").textContent = "Use a 32 character hexadecimal session ID.";
        return;
    }
    sessionStorage.setItem("maf-session", session.value);
    controller = new AbortController();
    busy(true);
    appendMessage("User", prompt);
    const answer = appendMessage("Assistant", "");
    element("status").textContent = "Generating";
    try {
        const response = await fetch(`${backend}/discussion${streaming ? "/flux" : ""}`, {
            method: "POST", headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ sessionId: session.value, message: prompt }), signal: controller.signal
        });
        if (!response.ok) throw new Error(`Backend returned HTTP ${response.status}.`);
        if (!streaming) answer.textContent = (await response.json()).message;
        else {
            const reader = response.body.getReader();
            const decoder = new TextDecoder("utf-8", { fatal: true });
            let pending = "";
            let completed = false;
            const consume = () => {
                let index;
                while ((index = pending.indexOf("\n")) >= 0) {
                    const frame = JSON.parse(pending.slice(0, index));
                    pending = pending.slice(index + 1);
                    if (frame.type === "text") answer.textContent += frame.text;
                    else if (frame.type === "completed") completed = true;
                    else if (frame.type === "error") throw new Error(frame.text);
                    else throw new Error("Unknown stream event.");
                }
            };
            try {
                while (true) {
                    const { value, done } = await reader.read();
                    if (done) break;
                    pending += decoder.decode(value, { stream: true });
                    consume();
                    element("messages").scrollTop = element("messages").scrollHeight;
                }
                pending += decoder.decode();
                consume();
                if (!completed || pending.length) throw new Error("Truncated stream.");
            }
            finally { await reader.cancel(); reader.releaseLock(); }
        }
        element("status").textContent = "Completed and saved";
    }
    catch (error) {
        element("status").textContent = error.name === "AbortError"
            ? "Canceled; completion unconfirmed. Partial display only." : `Failed; completion unconfirmed. ${error.message}`;
    }
    finally { controller = undefined; busy(false); }
}

element("stream").onclick = () => send(true);
element("complete").onclick = () => send(false);
element("cancel").onclick = () => controller?.abort();
element("new").onclick = () => {
    session.value = crypto.randomUUID().replaceAll("-", "");
    sessionStorage.setItem("maf-session", session.value);
    element("messages").replaceChildren();
    element("status").textContent = "New conversation. Previous snapshots remain on disk.";
};

