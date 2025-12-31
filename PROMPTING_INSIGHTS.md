# Prompting Insights

Observations from using AI assistants during TechStack Scanner development.

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

## 🚀 Recommendations for Future Projects

### 1. Detailed Architecture & Prompt Hierarchy Planning
Before writing any code, invest time in comprehensive planning:

**Architecture & Requirements:**
- Detailed architecture design with components, relationships, and data flows
- Clear understanding of project's end goal and its value/purpose
- Break down high-level goals into concrete, actionable tasks
- Define success criteria for each component and phase

**Prompt Hierarchy Creation:**
- Create not just a single guide, but an entire hierarchy of prompts:
  - **Level 0**: High-level project vision and architecture document
  - **Level 1**: Phase-by-phase implementation guide with checkpoints
  - **Level 2**: Component-specific detailed prompts with examples
  - **Level 3**: Refinement and optimization prompts for specific scenarios
- Each level references previous levels for context

**Essential Documentation:**
- Tech stack with exact versions
- Folder structure and file organization
- Step-by-step phases with dependencies
- Success criteria per phase
- Environment setup and prerequisites

**Why This Matters:**
- Reduces rework and refactoring
- Provides clear context for AI at every stage
- Enables jumping back to any phase if needed
- Creates reusable prompt patterns for similar projects

**Example Structure:**
```
documentation/
  ├── project-vision.md          # Level 0: Why, what, for whom
  ├── architecture-guide.md      # Level 0: System design
  ├── implementation-guide.md    # Level 1: Step-by-step execution
  ├── component-prompts/         # Level 2: Specific implementations
  │   ├── auth-setup.md
  │   ├── scanner-service.md
  │   └── ui-components.md
  └── optimization-prompts/      # Level 3: Refinements
      ├── performance.md
      └── testing.md
```

Reference appropriate level in each prompt: `#file:documentation/auth-setup.md`

---

### 2. Checkpoint Often
After every significant change:
- Run build
- Run tests
- Verify in browser/API client
- Commit to git with clear message

If something breaks, you know exactly which prompt caused it.

---

### 3. Use AI for Documentation Too
Don't write docs manually:
- Generate README sections with AI
- Have AI create setup instructions
- Ask AI to document complex logic with comments

---

### 4. Prefer Claude Sonnet 4.5 for Complex Tasks
Personal preference based on this project:
- Responses were more precise and accurate
- Output formatting and action explanations were more convenient for review
- Better at understanding complex multi-step requirements
- More consistent code quality across iterations

**Recommendation:** Start with Claude Sonnet 4.5 for architecture and complex features, use GPT models for simpler tasks.

---

### 5. TDD Approach Requires Better Architecture Planning
Test-Driven Development with AI can be highly effective, but requires:
- Clear upfront architecture design
- Well-defined interfaces and contracts before implementation
- Comprehensive test scenarios planned before coding
- More time investment in planning phase

**Recommendation:** For future projects, invest more time in architecture phase before starting TDD cycle with AI.

---

### 6. Don't Move Project Files During Active Session
**Critical:** Avoid moving or renaming project folders/files that are open and actively referenced in VS Code Copilot session.

**Risk:** Moving files can cause:
- Loss of session context
- AI losing track of file references
- Need to restart conversation with full context reload

**Recommendation:** Keep project structure stable during active development sessions. Plan folder structure in advance.

---

### 7. Consider MS 365 Copilot for Non-Coding Tasks
MS 365 Copilot doesn't use premium tokens from GitHub Copilot subscription.

**Use cases:**
- Initial project ideation and planning
- Documentation drafting
- Prompt refinement and optimization
- Architecture brainstorming

**Recommendation:** Use MS 365 Copilot for planning phases, GitHub Copilot for implementation.

---

### 8. Verify No Corporate/Machine Restrictions
Before starting AI-assisted development, ensure:
- No firewall blocking AI service endpoints
- No corporate policies preventing AI tool usage
- No proxy issues with authentication
- Full admin rights for installing dependencies
- Adequate disk space and system resources

**Risk:** Restrictions can cause unpredictable difficulties at any development stage or during agent work.

**Recommendation:** Test full AI access and development environment setup before committing to project timeline.

---

### 9. Leverage GitHub Awesome-Copilot Resources
The [github/awesome-copilot](https://github.com/github/awesome-copilot) repository is an invaluable resource for AI-assisted development.

**What it provides:**
- **Custom Instructions:** 100+ ready-to-use instruction files for various technologies (.NET, React, Python, etc.)
- **Prompt Templates:** Pre-built prompts for common development tasks
- **Chat Modes/Agents:** Specialized agents for specific workflows (DBA, code review, testing)
- **Best Practices:** Community-driven patterns and conventions
- **Real-world Examples:** Production-grade instruction files from experienced developers

**How to use:**
- Browse [instructions directory](https://github.com/github/awesome-copilot/tree/main/instructions) for your tech stack
- Copy relevant `.instructions.md` files to your `.github/instructions/` folder
- Adapt examples to your project's specific needs
- Contribute back your own instruction improvements

**Recommendation:** Start every new project by checking awesome-copilot for relevant instruction templates. Don't reinvent the wheel—adapt proven patterns.

---

### Final Note
The better your prompts, the better the results. Treat prompting as a skill to develop.
