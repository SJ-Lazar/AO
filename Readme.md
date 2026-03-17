
# Minimal CRM MVP

## Overview

This project is a minimal Customer Relationship Management (CRM) system designed for small businesses that need a simple way to manage customers, track deals, follow up on tasks, and monitor basic business activity without the complexity of a large enterprise platform.

The goal of this MVP is to deliver the smallest useful version of a CRM that helps teams stay organized, improve follow-up, and maintain visibility into sales progress.

## Problem Statement

Many small businesses still manage leads, contacts, and follow-ups using spreadsheets, inboxes, and scattered notes. This creates several problems:

- Customer information is difficult to keep consistent.
- Sales opportunities are easy to forget or lose track of.
- Tasks and follow-ups are not centralized.
- Reporting is manual and time-consuming.
- Team members do not always have a shared view of customer activity.

This CRM aims to solve those problems with a focused and practical first release.

## Delivery Channels

The solution now includes a user interface layer so the CRM can be delivered across multiple client experiences:

- A web UI for browser-based access
- A `.NET MAUI` UI for cross-platform app experiences
- A shared UI project for reusable components and common presentation logic

This allows the CRM MVP to evolve as a multi-channel product while keeping business logic centralized.

## MVP Goals

- Provide a centralized place to store customer and lead information.
- Allow users to track opportunities through a simple sales pipeline.
- Support task creation and follow-up reminders.
- Capture a basic activity history for important actions.
- Offer lightweight reporting for operational visibility.

## Target Users

This MVP is intended for:

- Small business owners
- Sales representatives
- Customer success teams
- Operations staff managing customer relationships

## Core Features

### 1. Contact Management

Users should be able to:

- Create, view, update, and archive contacts
- Store core information such as:
  - First name
  - Last name
  - Email address
  - Phone number
  - Company name
  - Job title
  - Notes
- Search and filter contacts
- View a simple interaction history per contact

### 2. Sales Pipeline Management

Users should be able to:

- Create sales opportunities or deals
- Assign a monetary value and expected close date
- Move deals through predefined stages such as:
  - Lead
  - Qualified
  - Proposal
  - Negotiation
  - Won
  - Lost
- Associate deals with contacts and companies
- View all open opportunities in one place

### 3. Task and Activity Tracking

Users should be able to:

- Create follow-up tasks related to a contact or deal
- Set due dates and mark tasks as completed
- Record activities such as:
  - Call logged
  - Meeting scheduled
  - Email sent
  - Note added
- Review pending and overdue work

### 4. Email Integration

For the MVP, email integration should remain lightweight and practical. Possible scope includes:

- Storing the primary email address for each contact
- Logging that an email was sent or received
- Saving email-related notes manually

For a first release, full two-way inbox synchronization is optional and can be deferred.

### 5. Reporting and Analytics

Users should be able to view basic metrics such as:

- Total contacts
- New contacts added over time
- Open deals by stage
- Won and lost deals
- Tasks due today or overdue
- Estimated pipeline value

The reporting layer should focus on simple summaries rather than complex dashboards.

### 6. User Interface

Users should be able to:

- Access the CRM through a browser-based interface
- Use the same core workflows from a `.NET MAUI` app
- Navigate contacts, deals, tasks, and summary views through a simple and consistent UI
- Reuse shared UI building blocks across platforms where possible

For the MVP, the interface should prioritize clarity, responsiveness, and completion of the core CRM workflows over advanced design customization.

## Suggested MVP Scope

To keep delivery focused, the MVP should include only the essential workflows:

### In Scope

- Contact CRUD
- Deal CRUD
- Pipeline stage updates
- Task CRUD
- Basic activity logging
- Simple reporting endpoints or pages
- Authentication for internal users
- Basic web UI for core CRM workflows
- Shared UI components that support both web and `.NET MAUI` clients

### Out of Scope for MVP

- Advanced workflow automation
- Full email client synchronization
- Marketing campaign management
- Role-based permissions with deep granularity
- Third-party marketplace integrations
- AI recommendations or forecasting
- Complex dashboard customization

## Example User Stories

- As a sales user, I want to create a contact so I can keep customer information in one place.
- As a sales user, I want to create a deal and move it through stages so I can track progress.
- As a user, I want to assign follow-up tasks so I do not miss customer actions.
- As a manager, I want to see open deals and overdue tasks so I can monitor execution.
- As a team member, I want to see contact activity history so I understand prior interactions.

## Proposed Domain Entities

The MVP can be built around a small set of core entities:

- `Contact`
- `Company`
- `Deal`
- `Task`
- `Activity`
- `User`

### Example Relationships

- A company can have many contacts.
- A contact can have many activities.
- A contact can be associated with many deals.
- A deal can have many tasks and activities.
- A user can own contacts, deals, and tasks.

## Typical MVP Workflows

### Contact Workflow

1. User creates a new contact.
2. User links the contact to a company.
3. User adds notes and communication details.
4. User schedules a follow-up task.

### Deal Workflow

1. User creates a deal for a contact or company.
2. User sets an initial stage and deal value.
3. User updates the stage as the opportunity progresses.
4. User marks the deal as won or lost.

### Task Workflow

1. User creates a task tied to a contact or deal.
2. User sets a due date.
3. User completes the task after follow-up.
4. System records the action in the activity history.

## Non-Functional Requirements

The MVP should also aim for:

- Clean and simple user experience
- Fast contact and deal lookup
- Reliable audit and activity tracking
- Basic security for business data
- API design that supports future extension
- Maintainable architecture for growth beyond MVP
- Reusable UI patterns across supported client applications

## Suggested Technical Direction

Given the current solution structure, this CRM can be organized as:

- `AO.API` for HTTP endpoints and authentication
- `AO.Core` for shared domain models, business logic, and cross-cutting concerns
- `AO.UI.Shared` for shared UI components and client-facing abstractions
- `AO.UI.Web` for the web front end
- `AO.UI` for the `.NET MAUI` client application
- `AO.Tests` for unit and integration test coverage

Potential implementation areas include:

- REST endpoints for contacts, deals, tasks, and reports
- Validation for incoming requests
- Idempotent handling for sensitive create operations where needed
- Audit trail support for business-critical changes
- Standardized API responses
- Shared presentation components for consistent CRM workflows across clients
- Client integration with the API from both web and `.NET MAUI` applications

## UI Architecture

The current UI structure supports a hybrid approach:

- `AO.UI.Shared` contains reusable UI code and shared services
- `AO.UI.Web` hosts the browser-based experience
- `AO.UI` provides the `.NET MAUI` application shell for supported platforms

This approach helps reduce duplicated UI logic while still allowing each client to provide platform-appropriate behavior when necessary.

## Getting Started

### API

- Run `AO.API` to expose the backend endpoints for the CRM
- Configure application settings as needed in `appsettings.json` or `appsettings.Development.json`

### Web UI

- Run `AO.UI.Web` to launch the browser-based client
- The web project references `AO.UI.Shared` for shared UI functionality

### .NET MAUI UI

- Run `AO.UI` to launch the cross-platform client
- The `.NET MAUI` project also references `AO.UI.Shared` to reuse shared UI functionality

## Current Solution Structure

- `AO.API` - backend API and authentication
- `AO.Core` - shared domain and infrastructure-oriented business logic
- `AO.UI.Shared` - reusable UI components and shared services
- `AO.UI.Web` - web front end
- `AO.UI` - `.NET MAUI` client
- `AO.Tests` - automated tests

## Milestones

### Phase 1: Foundation

- Set up project structure
- Define domain models
- Add persistence strategy
- Configure authentication and shared response models

### Phase 2: Core CRM

- Implement contact management
- Implement deal management
- Implement task tracking
- Add activity logging

### Phase 3: Visibility

- Add summary reporting
- Add filtering and search
- Improve validation and error handling
- Deliver UI views for the primary CRM workflows

### Phase 4: Hardening

- Add tests for core workflows
- Improve audit coverage
- Refine API contracts and documentation
- Improve shared UI reuse and cross-platform consistency

## Definition of Done for MVP

The MVP is complete when users can:

- Manage contacts from creation to update
- Track deals through a basic pipeline
- Create and complete follow-up tasks
- View activity history for customer interactions
- Access simple reports that summarize CRM activity
- Use the system from at least one working UI client backed by the API

## Future Enhancements

After the MVP, the product can expand with:

- Automated reminders and notifications
- Role-based access control
- Email synchronization
- File attachments
- Custom fields
- Dashboard widgets
- Forecasting and AI-assisted recommendations
- Expanded mobile and desktop experiences in the `.NET MAUI` client

## Summary

This minimal CRM MVP is focused on solving the most common small-business relationship management needs with a clear and practical scope. With the addition of shared web and `.NET MAUI` UI projects, the solution is now positioned to deliver those workflows through modern client experiences while keeping the backend and core business logic aligned for future growth.

