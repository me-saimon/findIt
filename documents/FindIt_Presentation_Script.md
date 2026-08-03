# FindIt — Presentation Script
### Speaker notes for a ~10–12 minute walkthrough (10 slides)

---

## Slide 1 — Title
**"Good [morning/afternoon] everyone. My name is [Your Name], and along with my teammate [Partner's Name], we're presenting our project for CSE 4106, Software Development Lab‑1, here at Kishoreganj University.**

**Our project is called FindIt — a centralized, smart Lost & Found Management System, built on ASP.NET Core MVC. Over the next few minutes, we'll walk you through the problem we're solving, how we're solving it, and our plan to build it."**

*(Pause briefly, then move to Slide 2.)*

---

## Slide 2 — Project Abstract
**"Let's start with why FindIt exists.**

**Losing personal belongings — a wallet, a phone, a laptop, an ID card — is something almost everyone has experienced, especially in busy places like universities, offices, and transit hubs. Right now, when that happens, recovery depends on scattered Facebook posts, WhatsApp groups, or a paper notice board. These methods are inefficient, and more often than not, people never get their belongings back.**

**FindIt solves this by centralizing the entire process into one web-based platform. Users can report a lost or found item, search an organized database, message each other securely, and submit ownership claims.**

**But the real innovation here is the smart matching engine — the system automatically compares lost and found reports based on category, location, and date, and recommends likely matches instead of making users search manually. Our overall goal is simple: make recovery faster, more transparent, and something people can trust."**

*(Move to Slide 3.)*

---

## Slide 3 — Problem Statement
**"To understand why this matters, let's break down exactly what's wrong with the current approach — four core problems.**

**First, Fragmentation. Information about lost and found items is spread across multiple, disconnected platforms — social media groups, physical boards — so the people who lost something and the people who found it almost never end up in the same place.**

**Second, Low Visibility. A notice board only reaches people walking past it, and a social media post gets buried within hours in a busy feed.**

**Third, Lack of Verification. Even when someone does try to return an item, there's no secure way to confirm the claimant actually owns it — which opens the door to items ending up with the wrong person.**

**And fourth, Inefficiency. Scrolling through unorganized posts to find a match is slow and manual — there's no automated way to connect a new report with an existing one.**

**These four gaps are exactly what FindIt is designed to close."**

*(Move to Slide 4.)*

---

## Slide 4 — Objectives
**"Given those problems, here's what we set out to achieve with this project — four clear objectives.**

**One, build a centralized, web-based platform using ASP.NET Core MVC where all lost and found reporting happens in one place.**

**Two, implement a smart matching algorithm — a weighted system that automatically connects a lost item with a found item based on category, location, and date.**

**Three, protect user trust and privacy through secure in-app messaging and a structured claim verification process, so personal contact details stay private until both sides agree to meet.**

**And four, make the whole experience fast and intuitive — a mobile-responsive interface that minimizes the time and effort it takes to get an item back to its owner."**

*(Move to Slide 5.)*

---

## Slide 5 — Scope of the Project
**"Every project needs clear boundaries, so let's talk about scope — what version one will include, and what it won't.**

**On the in-scope side: a responsive web application with role-based access for Admins and Registered Users; secure registration and login through ASP.NET Core Identity; modules for reporting lost or found items, including image uploads and categorization; the smart matching engine with automated notifications; a secure claiming workflow that requires proof of ownership; and an admin dashboard for moderation and resolving disputes.**

**On the out-of-scope side, for this initial version: we won't be building native iOS or Android apps — the web app is mobile-responsive instead, which covers that need. We also won't integrate physical IoT trackers like RFID or Bluetooth tags, and there's no monetary reward or payment system in this version.**

**These are deliberate trade-offs to keep the first release focused and achievable within our timeline — and they're things we can revisit in a future version once the platform proves itself."**

*(Move to Slide 6.)*

---

## Slide 6 — Methodology
**"Now let's talk about how we're actually going to build this.**

**We're following the Agile methodology, which means iterative progress with continuous testing rather than one long build-and-release cycle.**

**On the architecture side, we're using the Model-View-Controller pattern, which cleanly separates our application logic, our user interface, and our data models.**

**For the backend, we're using ASP.NET Core on .NET 8, with C# handling all the server-side logic.**

**Our database is Microsoft SQL Server, managed through Entity Framework Core using a Code-First approach — that covers our core data: Users, Items, Claims, and Messages.**

**On the frontend, we're using Razor Views along with HTML5, CSS3, Bootstrap 5, and JavaScript with AJAX calls for a dynamic, responsive experience.**

**And finally, the matching logic — a weighted scoring algorithm that runs whenever a new item is submitted, comparing tags and attributes against existing reports to suggest the most likely matches."**

*(Move to Slide 7.)*

---

## Slide 7 — Project Timeline
**"Here's how we're planning to spread that work out — a 10-week development schedule.**

**We start with Requirement Analysis and writing the SRS in week one, then move into System Design — ERD, use cases, and UI wireframes — across weeks one and two.**

**From there, we set up the database and the ASP.NET project in weeks two and three, followed by Authentication and User Management in weeks three and four.**

**The core Lost & Found Reporting module comes in weeks four and five, and Search, Filtering, and Notifications follow in weeks five and six.**

**The Smart Matching Algorithm — our key differentiator — is built in weeks six and seven, followed by Claim Management and Messaging in weeks seven and eight.**

**We wrap up with the Admin Dashboard and Reports in weeks eight and nine, and finish with Testing, Deployment, and Documentation in the final two weeks.**

**Each module builds on the last, which fits naturally with our Agile approach."**

*(Move to Slide 8.)*

---

## Slide 8 — Budget
**"One of the strengths of this project is that it's essentially free to build.**

**Every tool in our stack — Visual Studio Community Edition, SQL Server Express, Git and GitHub for version control, Bootstrap 5, Leaflet.js with OpenStreetMap for mapping, Chart.js for visualizations, SQL Server Management Studio, and local testing through IIS Express or Kestrel — is open-source or free-tier.**

**That brings our total development cost to zero dollars for a full year of operation. No licenses, no hosting fees, no physical materials — which means our budget risk for this project is effectively nonexistent."**

*(Move to Slide 9.)*

---

## Slide 9 — Risk Analysis
**"Of course, no project is risk-free, so we've identified four key risks and how we plan to handle each one.**

**Low User Adoption — if the platform doesn't reach a critical mass of users, items may go unmatched. We're mitigating this with an intuitive UI, guest browsing for found items, and a planned partnership with campus security to funnel physical lost-and-found items into the digital system.**

**False Claims or Fraud — someone might try to claim an item that isn't theirs. To prevent that, the finder approves each claim based on hidden identifiers only the true owner would know — like a laptop's wallpaper or a specific scratch — and admins have override power in disputes.**

**Data Privacy Breach — exposing user contact details is a real concern. We handle this with secure password hashing through ASP.NET Core Identity, enforced HTTPS, and keeping all early communication inside in-app messaging so personal numbers and emails stay private until users agree to meet.**

**And Technical Performance — as the database grows, the matching algorithm could slow down. We're addressing this with optimized SQL queries, indexing on tags and categories, and running the matching logic asynchronously."**

*(Move to Slide 10.)*

---

## Slide 10 — Conclusion
**"To wrap up — FindIt is a modern, practical solution to a problem almost everyone deals with at some point.**

**By replacing scattered social media posts and physical notice boards with one centralized, intelligent platform built on ASP.NET Core MVC, we believe this project can dramatically improve the odds of people actually getting their belongings back.**

**The smart matching engine and the secure claim verification process aren't just technical features — they're what make the system genuinely useful and trustworthy. With the right development and deployment, we think FindIt has real potential to become a standard utility for universities, offices, and public spaces well beyond our campus.**

**Thank you — we're happy to take any questions."**

*(End on Slide 10, open the floor for Q&A.)*

---

### Delivery tips
- **Pace:** ~60–75 seconds per content slide (2–9), ~30–40 seconds each for the title and conclusion — totals roughly 10–12 minutes.
- **Eye contact:** Look up from notes during the bolded opening line of each slide; that's the "hook" the audience should hear delivered naturally.
- **Transitions:** Use the bracketed cues (*"Move to Slide X"*) as your mental clicker — don't read them aloud.
- **Q&A prep:** Be ready to go deeper on the matching algorithm's weighting logic and the claim-verification workflow — these are the two most common follow-up questions for this type of project.
