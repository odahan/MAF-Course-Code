# Browser client

Start the shared backend at http://localhost:5088 and this project at http://localhost:5089. The backend permits exactly that browser origin by default. Native WPF and console clients do not use CORS.

The page supports complete responses, streaming, cancellation and an editable session ID. Successful turns persist in the backend; the browser display is local and is not reconstructed from the server. Session storage retains only the ID across reloads. New conversation changes the ID and clears the display; it does not delete previous snapshots.

Use `/incident INC-318` to exercise the actual local MCP tool. See [the common guide](../../../docs/lot-012/Streaming.md).
