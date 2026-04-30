## Project Requirements Document (PRD) - CineBase: Design & UI/UX Phase

**Project Name:** CineBase
**Document Version:** 1.0
**Date:** April 6, 2026
**Author:** Prof. Gennaro Malafronte con l'uso di Google Stitch

---

### 1. Executive Summary

CineBase is envisioned as a cutting-edge management platform for cinema networks. Its primary goal is to provide advanced technical functionalities for managing films, directors, and projections, while simultaneously upholding a **premium, modern, and artistic aesthetic**. This document outlines the foundational design principles, visual identity, and UI/UX guidelines that will shape the platform, ensuring a uniform, professional, and intuitive user experience. This phase focuses specifically on defining the comprehensive System Design and UI Kit to guide all subsequent development.

### 2. Project Vision & Goals

**Vision:** To be the leading cinema network management platform, celebrated for its robust functionality, elegant design, and seamless user experience, reflecting the prestige and artistry of the cinematic world.

**Goals for this Phase (Design & UI/UX):**
*   Establish a consistent and premium visual identity across all platform modules.
*   Develop a comprehensive UI Kit and Style Guide to serve as the single source of truth for all design and development.
*   Ensure clarity, intuitiveness, and efficiency in user interactions.
*   Lay the groundwork for scalable and maintainable front-end development.

### 3. Target Audience

The primary users of CineBase are **operators and managers of cinema networks**. They require a powerful, efficient, and visually appealing tool to manage complex data and operations related to film scheduling, director information, and projection logistics.

### 4. Scope of Work (Current Phase: System Design & UI Kit)

This phase focuses exclusively on the design and UI/UX architecture for CineBase. It includes:

*   **Definition of Core Design Principles:** Vision, UX, and UI guidelines.
*   **Full Visual Identity Specification:** Color palette, typography, graphic elements.
*   **Comprehensive System Design & UI Kit:** Detailing all reusable UI components.
*   **Standard Page Layout Structures:** Defining the framework for all main application views.

**Out of Scope for this Phase:** Backend development, full functional implementation, detailed feature specifications beyond design needs.

### 5. High-Level Features (Implied by Design)

While the full functional scope is beyond this design document, the UI/UX design will support:

*   **Dashboard:** Overview of key metrics and operational summaries.
*   **Film Management:** Listing, adding, editing, and managing film details.
*   **Director Management:** Managing director profiles and associated films.
*   **Projection Management:** Scheduling and managing cinema screenings.
*   **Global Search & Notifications:** System-wide search and alerts for critical events.
*   **User Profile Management:** Basic user account settings.

### 6. Design & UI/UX Specifications

#### 6.1. Design Vision & Principles

*   **Aesthetic:** Premium, modern, uniform, artistic, reflecting the elegance of grand cinema.
*   **Interface:** Clean, spacious, highly professional.
*   **UX Principles:**
    *   **Uniformity:** Consistent navigation and visual hierarchy across all pages.
    *   **Clarity:** Generous use of whitespace to reduce cognitive load.
    *   **Hierarchy:** Critical information (KPIs, metrics) placed prominently in summary cards, followed by operational details.

#### 6.2. Visual Identity (UI)

*   **6.2.1. Color Palette:** Designed for authority, technology, and discrete luxury.
    *   **Primary (Indigo Dark):** `#1A1B2E` - Sidebar, main navigation.
    *   **Accent (Gold/Amber):** `#D4AF37` or `#FFBF00` - CTAs, active icons, success states.
    *   **Background (Light Gray):** `#F8F9FA` - Main workspace, maximum readability.
    *   **Text (Charcoal):** `#2D2D2D` - Body text, main titles.
    *   **Status Colors:**
        *   **Cyan:** For technical data, live information.
        *   **Emerald:** For confirmations, positive states.
        *   *(Future Consideration: Error states - e.g., Red, Warning states - e.g., Orange)*

*   **6.2.2. Typography:**
    *   **Headings (H1-H6):** Geometric Sans-serif (e.g., *Montserrat* or *Inter*). Weights: Bold (700) for section titles, Light (300) for artistic touches.
    *   **Body Text:** Clean Sans-serif (e.g., *Inter* or *Roboto*). Weight: Regular (400).
    *   **Data/Technical:** Monospaced font (e.g., *JetBrains Mono*) for projection codes, timestamps (optional, for specific data display).
    *   **Typography Scale:** A defined scale for H1-H6, paragraph, and small text sizes will be part of the UI Kit.

*   **6.2.3. Graphic Elements:**
    *   **Cards:** White background, rounded borders (8px-12px), subtle shadow (`box-shadow: 0 4px 6px rgba(0,0,0,0.05)`).
    *   **Sidebar:** Dark "Glassmorphism" effect, thin outline icons.
    *   **Tables:** Clean rows, light hover effect, film/director thumbnails for visual impact.
    *   **Shadow Styles:** Defined levels (Soft, Medium, Deep) for consistency.
    *   **Grid System:** Clear guidelines for spacing and layout structure.

#### 6.3. Standard Page Layout Structure

All management pages will adhere to the following schema:
1.  **Left Sidebar:** Fixed navigation with CineBase logo.
2.  **Top Bar:** Global search, notifications, user profile.
3.  **Page Header:** Section title, primary action button (e.g., "Add Film") in gold.
4.  **Metric Cards Row:** 3-4 statistical data cards.
5.  **Main Content:** Data table or management grid with advanced filters.

#### 6.4. UI Component Library (Detailed in UI Kit)

The UI Kit will detail the design and specifications for:

*   **Buttons:** Primary (Gold), Secondary (Indigo), Outline buttons, various states (hover, active, disabled).
*   **Input Fields:** Text inputs, text areas, search bars (normal, focus, error states).
*   **Dropdowns & Selects:** Standard dropdowns, multi-select.
*   **Status Badges:** Utilizing Cyan (technical/live) and Emerald (success/positive).
*   **Cards:** Various configurations for metrics, content, etc.
*   **Tables:** Standard data tables, sortable columns, pagination.
*   **Icons:** Thin outline icons for navigation and actions.

### 7. Success Metrics (for Design Phase)

*   **Consistency Score:** Quantitative measure (e.g., automated tools) or qualitative (e.g., design review) of adherence to the UI Kit across developed pages.
*   **Developer Adoption:** Ease of use and clear understanding of the UI Kit by developers.
*   **Positive Feedback from Stakeholders:** Approval of the overall aesthetic and user experience by key project stakeholders.
*   **Roadmap Completion:** Successful implementation of defined aesthetic roadmap items.

### 8. Future Considerations & Roadmap (Design & UI/UX)

*   **Micro-interactions:** Implement subtle animations and hover effects, especially on primary CTAs.
*   **Mobile Responsiveness:** Develop responsive layouts for all pages while maintaining the minimalist style.
*   **Error & Loading States:** Define specific UI for error messages, empty states, and skeleton loading screens.
*   **Dark Mode:** Explore and define a variant of the System Design for a Dark Mode theme.
*   **Custom Brand Icons:** Define and integrate unique custom icons where appropriate.
*   **Accessibility:** Incorporate WCAG guidelines into the design system.

### 9. Dependencies & Assumptions

*   **Design Tool Availability:** Access to design software (e.g., Figma, Sketch, Adobe XD) for creating and documenting the UI Kit.
*   **Developer Collaboration:** Close collaboration between design and front-end development teams is crucial for successful implementation.
*   **Stakeholder Feedback:** Timely and constructive feedback from project stakeholders on design deliverables.

---