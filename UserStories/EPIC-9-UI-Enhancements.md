# Epic 9: UI Enhancements for Work Description

## Goal
Add UI components to allow users to optionally enter and view work descriptions when punching in/out.

---

## US-901: Add Work Description Input Field to Home Page

**As a** user
**I want** to optionally enter a description of my work when punching in or out
**So that** I can track what I was working on during each punch session

### Acceptance Criteria
- [ ] Add optional text input field to Home page for work description
- [ ] Position field near punch in/out buttons
- [ ] Use MudBlazor text field component for consistency
- [ ] Field should be multi-line (textarea) with max 1000 characters
- [ ] Add character counter showing remaining characters
- [ ] Field should be optional - users can punch without description
- [ ] Clear field after successful punch
- [ ] Handle empty/whitespace-only input gracefully

### Technical Notes
```razor
<MudTextField
    @bind-Value="workDescription"
    Label="Work Description (Optional)"
    Variant="Variant.Outlined"
    Lines="3"
    MaxLength="1000"
    Counter="1000"
    Placeholder="What are you working on?" />
```

### Files to Modify
- `src/TimeClockUI/Pages/Home.razor`
- `src/TimeClockUI/Pages/Home.razor.cs` (if code-behind exists)

---

## US-902: Update Punch Method to Include Work Description

**As a** developer
**I want** the punch method to send work description to the API
**So that** work descriptions are saved with punch records

### Acceptance Criteria
- [ ] Update `Punch()` method in Home.razor to accept work description
- [ ] Pass work description to `TimePunchClient.Punch()` method
- [ ] Trim whitespace from description before sending
- [ ] Convert empty string to null before sending
- [ ] Verify API accepts and stores the work description
- [ ] Handle API errors related to description (e.g., too long)

### Technical Notes
```csharp
var description = string.IsNullOrWhiteSpace(workDescription)
    ? null
    : workDescription.Trim();
await timePunchClient.Punch(selectedHourType, PunchType.PunchIn, description);
```

### Files to Modify
- `src/TimeClockUI/Pages/Home.razor`
- `src/TimeClock.client/Class1.cs` (update Punch method signature)
- `src/TimeApi/Api/TimePunchController.cs` (ensure it accepts WorkDescription)

---

## US-903: Display Work Descriptions in Punch History

**As a** user
**I want** to see work descriptions in my punch history
**So that** I can review what I worked on previously

### Acceptance Criteria
- [ ] Display work description for each punch record on Home page
- [ ] Show description below or next to the punch time
- [ ] Use different styling (e.g., italic, lighter color) to distinguish from time
- [ ] Handle null/empty descriptions gracefully (don't show empty space)
- [ ] Truncate long descriptions with "..." and show full text on hover/expand
- [ ] Ensure descriptions don't break layout on mobile devices

### Technical Notes
```razor
@if (!string.IsNullOrWhiteSpace(punch.WorkDescription))
{
    <MudText Typo="Typo.body2" Class="mt-1" Style="font-style: italic; color: gray;">
        @punch.WorkDescription
    </MudText>
}
```

### Files to Modify
- `src/TimeClockUI/Pages/Home.razor`

---

## US-904: Add Work Description to Week/Month Summary Pages

**As a** developer
**I want** work descriptions available in week and month summary views
**So that** users can review their work over time (when those pages are implemented)

### Acceptance Criteria
- [ ] Ensure `PunchRecord` DTO includes WorkDescription in API responses
- [ ] Add placeholder/comment in WeekSummary.razor for future implementation
- [ ] Add placeholder/comment in MonthSummary.razor for future implementation
- [ ] Document the data structure for future developers

### Technical Notes
- This story is for future-proofing
- Week/Month summary pages are currently stubs and will be implemented later
- WorkDescription should be part of the data returned by date range queries

### Files to Modify
- `src/TimeClockUI/Pages/WeekSummary.razor`
- `src/TimeClockUI/Pages/MonthSummary.razor`

---

## Definition of Done (Epic 9)
- [ ] Users can enter work description when punching in/out
- [ ] Work description is optional and validation works correctly
- [ ] Work descriptions are saved to database
- [ ] Work descriptions display in punch history on Home page
- [ ] Empty/null descriptions handled gracefully
- [ ] Character limit enforced (1000 chars)
- [ ] UI is responsive and works on mobile devices
- [ ] Placeholders added for future week/month summary pages
- [ ] Code reviewed and merged to main branch
