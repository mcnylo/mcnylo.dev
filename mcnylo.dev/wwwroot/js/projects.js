function initializeProjectsPage() {

    const filter = document.getElementById("project-filter-form");
    const results = document.getElementById("project-results");

    if (!filter || !results) {
        return;
    }

    setupFilterCountUpdates();
    setupProjectDropdownBehavior();
    setupClearFiltersButton();
    setupProjectPagination();
    updateAllFilterCounts();

    filter.addEventListener("submit", handleProjectSearch);
}

function setupFilterCountUpdates() {
    const filterCheckboxes = document.querySelectorAll(".project-category-checkbox, .project-tag-checkbox");

    filterCheckboxes.forEach((checkbox) => {
        checkbox.addEventListener("change", updateAllFilterCounts);
    });
}

function updateAllFilterCounts() {
    updateFilterCount(
        ".project-category-checkbox",
        "selected-category-count",
        "Select categories",
        "1 category selected",
        "categories selected"
    );

    updateFilterCount(
        ".project-tag-checkbox",
        "selected-tag-count",
        "Select tags",
        "1 tag selected",
        "tags selected"
    );
}

function updateFilterCount(checkboxSelector, labelId, defaultText, singleText, pluralText) {
    const label = document.getElementById(labelId);

    if (!label) {
        return;
    }

    const checkedCount = document.querySelectorAll(`${checkboxSelector}:checked`).length;

    if (checkedCount === 0) {
        label.textContent = defaultText;
        return;
    }

    if (checkedCount === 1) {
        label.textContent = singleText;
        return;
    }

    label.textContent = `${checkedCount} ${pluralText}`;
}

async function handleProjectSearch(event) {
    event.preventDefault();

    setProjectPageNumber(1);

    await searchProjects();
}

function buildProjectSearchUrl(filter) {
    const formData = new FormData(filter);
    const query = new URLSearchParams(formData).toString();

    return `${filter.action}?${query}`;
}

async function fetchProjectResults(url) {
    const response = await fetch(url, {
        method: "GET",
        headers: {
            "X-Requested-With": "fetch"
        }
    });

    if (!response.ok) {
        throw new Error("Project search failed!");
    }

    return await response.text();
}

function setProjectSearchLoadingState(results, submitButton, isLoading, buttonText = "Search Projects") {
    results.classList.toggle("opacity-50", isLoading);

    if (!submitButton) {
        return;
    }

    submitButton.disabled = isLoading;
    submitButton.textContent = isLoading ? "Searching..." : buttonText;
}

function setupClearFiltersButton() {
    const clearButton = document.getElementById("clear-project-filters");

    if (!clearButton) {
        return;
    }

    clearButton.addEventListener("click", async () => {
        clearProjectFilters();
        updateAllFilterCounts();

        await searchProjects();
    });
}

function clearProjectFilters() {
    const filter = document.getElementById("project-filter-form");

    if (!filter) {
        return;
    }

    const searchInput = filter.querySelector("input[name='Search']");
    const checkedBoxes = filter.querySelectorAll("input[type='checkbox']:checked");

    if (searchInput) {
        searchInput.value = "";
    }

    checkedBoxes.forEach((checkbox) => {
        checkbox.checked = false;
    });
}

async function searchProjects() {
    const filter = document.getElementById("project-filter-form");
    const results = document.getElementById("project-results");

    if (!filter || !results) {
        return;
    }

    closeAllProjectDropdowns();

    const submitButton = filter.querySelector("button[type='submit']");
    const originalButtonText = submitButton?.textContent ?? "Search Projects";
    const searchUrl = buildProjectSearchUrl(filter);

    try {
        setProjectSearchLoadingState(results, submitButton, true);

        const html = await fetchProjectResults(searchUrl);

        results.innerHTML = html;
    }
    catch (error) {
        console.error(error);

        results.innerHTML = `
            <div class="alert alert-error">
                <span>Could not load projects. Try again in a second.</span>
            </div>
        `;
    }
    finally {
        setProjectSearchLoadingState(results, submitButton, false, originalButtonText);
    }
}

function setupProjectDropdownBehavior() {
    const dropdowns = document.querySelectorAll(".project-filter-dropdown");

    dropdowns.forEach((dropdown) => {
        dropdown.addEventListener("toggle", () => {
            if (dropdown.open) {
                closeOtherProjectDropdowns(dropdown);
            }
        });
    });

    document.addEventListener("click", (event) => {
        const clickedInsideDropdown = event.target.closest(".project-filter-dropdown");

        if (clickedInsideDropdown) {
            return;
        }

        closeAllProjectDropdowns();
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            closeAllProjectDropdowns();
        }
    });
}

function closeOtherProjectDropdowns(currentDropdown) {
    const openDropdowns = document.querySelectorAll(".project-filter-dropdown[open]");

    openDropdowns.forEach((dropdown) => {
        if (dropdown !== currentDropdown) {
            dropdown.removeAttribute("open");
        }
    });
}

function closeAllProjectDropdowns() {
    const openDropdowns = document.querySelectorAll(".project-filter-dropdown[open]");

    openDropdowns.forEach((dropdown) => {
        dropdown.removeAttribute("open");
    });
}

function setupProjectPagination() {
    const results = document.getElementById("project-results");

    if (!results) {
        return;
    }

    results.addEventListener("click", async (event) => {
        const pageButton = event.target.closest("[data-project-page]");

        if (!pageButton || pageButton.classList.contains("btn-disabled")) {
            return;
        }

        event.preventDefault();

        setProjectPageNumber(pageButton.dataset.projectPage);

        await searchProjects();
    });
}

function setProjectPageNumber(pageNumber) {
    const pageNumberInput = document.getElementById("project-page-number");

    if (!pageNumberInput) {
        return;
    }

    pageNumberInput.value = pageNumber;
}

initializeProjectsPage();