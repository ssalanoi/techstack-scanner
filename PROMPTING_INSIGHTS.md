# Prompting Insights & Learnings

Observations from using AI assistants during TechStack Scanner development.

---

## 🎯 Primary Prompting Techniques Used

Throughout the development of TechStack Scanner, three main prompting approaches were employed:

### 1. Zero-Shot Prompting
**Definition:** Providing a task to the AI without any prior examples, relying solely on detailed instructions.

**When Used:**
- Initial project scaffolding and setup
- Creating new components or services from scratch
- Generating configuration files
- Implementing well-established patterns (JWT auth, CORS setup)

**Example:**
```
Create a C# service that:
- Accepts a folder path
- Scans for: package.json, *.csproj, requirements.txt, Gemfile
- Extracts framework names and versions
- Returns structured data
```

**Why It Worked:** Clear, comprehensive instructions with specific requirements allowed AI to generate correct implementations without needing examples.

---

### 2. Few-Shot Prompting
**Definition:** Providing one or more examples to guide the AI's output format and style.

**When Used:**
- Adding similar parsers for different package managers
- Extending existing functionality with new formats
- Creating components with consistent patterns
- Writing tests following established structure

**Example:**
```
Add Ruby detection (Gemfile) to ScanService following the same pattern 
as npm detection in apps/api/Services/ScanService.cs lines 45-60.
```

**Why It Worked:** Referencing existing implementations ensured consistency across codebase and reduced need for detailed specifications.

---

### 3. Interactive Prompting
**Definition:** Iterative dialogue where each prompt builds on previous responses, with verification at each step.

**When Used:**
- Debugging complex issues
- Refining implementations based on test results
- Multi-step feature development
- Configuration troubleshooting

**Example Workflow:**
```
1. "Implement scan queue service"
   → AI provides implementation
2. "Test fails with NullReferenceException at line 45"
   → AI provides fix
3. "Works but scan status not updating in UI"
   → AI identifies missing SignalR setup
4. "SignalR added, verify the flow"
   → AI confirms implementation
```

**Why It Worked:** 
- Allowed course correction without starting over
- Leveraged AI's ability to maintain context across conversation
- Enabled incremental refinement with validation at each step
- Reduced risk of large-scale refactoring

---

### Technique Distribution
- **Zero-Shot:** ~40% of prompts (new features, initial implementations)
- **Few-Shot:** ~35% of prompts (pattern replication, extensions)
- **Interactive:** ~25% of prompts (debugging, refinements, troubleshooting)

---

## 📊 What Worked Well

### 1. Structured Initial Prompts
**Pattern:** Provide comprehensive context upfront with clear constraints and deliverables.

**Example:**
```
Hard constraints:
- Follow guide exactly
- Use specific tech stack (React 18, .NET 9, etc.)
- Windows PowerShell commands with ";" not "&&"
- Chain commands with ";" not "&&"

Process rules:
1) Implement ONE step at a time
2) For each step: Plan → Commands → Files → Implementation → Verification → STOP
```

**Why it worked:** AI had clear boundaries, preventing feature creep and maintaining focus.

---

### 2. Step-by-Step Execution with Checkpoints
**Pattern:** Break complex work into phases, require verification after each step.

**Example:**
```
Now start with Phase 1 – Step 1.1 (Monorepo scaffolding).
After completion, STOP and ask me to confirm results before proceeding.
```

**Why it worked:** 
- Prevented cascading errors
- Allowed course correction early
- Made debugging easier
- Kept implementation aligned with requirements

---

### 3. Explicit Technology Versions
**Pattern:** Specify exact versions for all major dependencies.

**Example:**
```
- React 18 + TypeScript, Vite 6, React Router 7, Mantine 7, TanStack Query v5
- ASP.NET Core 9+ Web API, EF Core 9 + SQLite
```

**Why it worked:** Eliminated version conflicts and compatibility issues.

---

### 4. OS-Specific Command Patterns
**Pattern:** Explicitly state OS and command syntax requirements.

**Example:**
```
Windows PowerShell commands only. IMPORTANT: chain commands with ";" not "&&".
```

**Why it worked:** Avoided cross-platform confusion, commands worked first try.

---

### 5. File-by-File Implementation Requests
**Pattern:** Ask for complete file content in structured format.

**Example:**
```
For each step, provide:
a) Plan (3-6 bullets)
b) Commands to run
c) Files you will create/modify
d) Implementation (full code)
```

**Why it worked:** Clear what to create, where, and how; nothing ambiguous.

---

## ❌ What Didn't Work & Why

### 1. Vague Feature Requests
**Anti-pattern:** "Add authentication" or "Make it work with Docker"

**Problem:** AI made assumptions about:
- Auth mechanism (OAuth vs JWT vs cookies)
- Storage (where to put tokens, session management)
- Docker setup (single container vs multi-container)

**Solution:** Be specific: "Add JWT Bearer auth with Admin role, credentials from env vars, token in localStorage under 'tss-token'"

---

### 2. Assuming AI Knows Project State
**Anti-pattern:** "Fix the login issue" without context.

**Problem:** AI didn't know:
- What error occurred
- Which file has the issue
- What was tried before

**Solution:** Provide context: "Login returns 401 in [AuthController.cs](apps/api/Controllers/AuthController.cs#L25), env vars are set, JWT_SECRET is 32 chars. Here's the error log: [paste]"

---

### 3. Multiple Changes in One Prompt
**Anti-pattern:** "Add Docker support, fix CORS, and update README"

**Problem:** 
- Lost track of what succeeded vs failed
- Hard to verify each piece
- Difficult to rollback if one part broke

**Solution:** One change per prompt, verify, then next change.

---

### 4. Skipping Verification Steps
**Anti-pattern:** Accepting all suggestions without running/testing.

**Problem:**
- Configuration errors propagated
- Imports broke in later steps
- Hard to pinpoint when things went wrong

**Solution:** Run verification commands after every step, paste output to AI before proceeding.

---

### 5. Not Specifying File Paths
**Anti-pattern:** "Update the config file"

**Problem:** Multiple config files exist (eslint.config.js, vite.config.ts, appsettings.json)

**Solution:** Use exact paths: "Update [apps/web/vite.config.ts](apps/web/vite.config.ts)"

---

## 🎯 Best Prompting Patterns

### Pattern 1: Architecture → Implementation → Tests
**Workflow:**
1. First prompt: "Propose architecture for [feature]"
2. Review and approve
3. Second prompt: "Implement [component] following approved architecture"
4. Third prompt: "Add tests for [component] covering [scenarios]"

**Benefits:** Logical progression, each step builds on previous, easy to review.

---

### Pattern 2: Error-Driven Refinement
**Workflow:**
1. Try implementation
2. Get error
3. Prompt: "Here's the error: [paste]. In [file.ts](path/file.ts#L50). Here's the code: [snippet]. How to fix?"

**Benefits:** Precise diagnosis, focused fix, no over-refactoring.

---

## 🚀 Recommendations for Future Projects

### 1. Start with Comprehensive Guide
Create a detailed guide document before coding:
- Tech stack with versions
- Folder structure
- Step-by-step phases
- Success criteria per phase

Reference it in every prompt: `#file:guide.md`

---

### 2. Use Session-Based Development
Group related prompts into sessions:
- **Session goal** at the start
- **Session outcome** at the end
- Verify before moving to next session

---

### 3. Maintain Prompt Log in Real-Time
Don't wait until end to document:
- Copy prompt after sending
- Paste response summary
- Note acceptance rate immediately
- Track manual edits as you make them

---

### 4. Checkpoint Often
After every significant change:
- Run build
- Run tests
- Verify in browser/API client
- Commit to git with clear message

If something breaks, you know exactly which prompt caused it.

---

### 5. Use AI for Documentation Too
Don't write docs manually:
- Generate README sections with AI
- Have AI create setup instructions
- Ask AI to document complex logic with comments

---

## 💡 Key Takeaways

1. **Specificity matters**: Exact versions, file paths, and constraints eliminate ambiguity.
2. **Incremental wins**: Small verified steps > large unverified leaps.
3. **Context is king**: AI performs best with full picture of project state.
4. **Verification is mandatory**: Never assume generated code works without testing.
5. **Patterns over instructions**: Reference existing code patterns when possible.
6. **Document as you go**: Prompt log becomes invaluable for understanding evolution.
7. **Architecture first**: Planning before implementation saves refactor time.
8. **Error messages are goldmines**: Paste full errors for precise fixes.

---

### Final Note
The better your prompts, the better the results. Treat prompting as a skill to develop.
