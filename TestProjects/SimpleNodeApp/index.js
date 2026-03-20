const express = require("express");
const app = express();
const PORT = 3000;

app.get("/", (req, res) => {
  res.json({
    status: "running",
    project: "simple-node-app",
    pid: process.pid,
    uptime: process.uptime(),
    memoryUsage: process.memoryUsage(),
  });
});

app.get("/health", (req, res) => {
  res.json({ status: "healthy", timestamp: new Date().toISOString() });
});

app.listen(PORT, () => {
  console.log(`Simple Node App listening on port ${PORT}`);
});
