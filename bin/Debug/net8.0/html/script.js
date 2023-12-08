// Helper functions
function toggleDisplay(element, isShow) {
    element.style.display = isShow ? 'block' : 'none';
}

function createElementWithText(xmlDoc, tagName, textContent) {
    const element = xmlDoc.createElement(tagName);
    element.textContent = textContent;
    return element;
}

function loadInitialConfig() {
    if (localStorage.getItem('darkMode') === 'true') {
        document.body.classList.add('dark-mode');
        document.getElementById('toggleDarkMode').textContent = 'Light Mode';
    }
}

// Main functions
function setupToggleButtons() {
    const toggleConsoleButton = document.getElementById('toggleConsole');
    const consoleOutput = document.querySelector('.console-output');
    toggleConsoleButton.addEventListener('click', () => {
        const isHidden = consoleOutput.style.display === 'none';
        toggleDisplay(consoleOutput, isHidden);
        toggleConsoleButton.textContent = isHidden ? 'Hide Console' : 'Show Console';
    });

    const toggleDarkModeButton = document.getElementById('toggleDarkMode');
    toggleDarkModeButton.addEventListener('click', () => {
        document.body.classList.toggle('dark-mode');
        const mode = document.body.classList.contains('dark-mode');
        localStorage.setItem('darkMode', mode);
        toggleDarkModeButton.textContent = mode ? 'Light Mode' : 'Dark Mode';
    });
}

// Call setup functions on DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
    setupToggleButtons();
    loadInitialConfig();
    setInterval(updateSensorData, 1000);
});

function rpmToPercentage(rpm) {
    // Mapping RPM to percentage based on the data you provided
    const rpmPercentages = [
        { rpm: 3720, percent: 10 },
        { rpm: 5160, percent: 20 },
        { rpm: 6600, percent: 30 },
        { rpm: 8280, percent: 40 },
        { rpm: 9778, percent: 50 },
        { rpm: 11292, percent: 60 },
        { rpm: 12807, percent: 70 },
        { rpm: 14280, percent: 80 },
        { rpm: 15836, percent: 90 },
        { rpm: 17640, percent: 100 }
    ];

    // Find the closest RPM percentage
    let closest = rpmPercentages.reduce((prev, curr) => {
        return (Math.abs(curr.rpm - rpm) < Math.abs(prev.rpm - rpm) ? curr : prev);
    });

    return closest.percent;
}

function updateSensorData() {
    fetch('sensors.xml')
        .then(response => response.text())
        .then(str => (new window.DOMParser()).parseFromString(str, "text/xml"))
        .then(data => {
            // Update fans
            for (let i = 1; i <= 6; i++) {
                const fanSpeed = data.querySelector(`Fan${i}`).textContent;
                const fanPercentage = rpmToPercentage(parseInt(fanSpeed, 10));
                document.getElementById(`fan${i}RPM`).textContent = `RPM: ${fanSpeed}`;
                document.getElementById(`fan${i}PCT`).textContent = `PCT: ${fanPercentage}%`; // Update the percentage text
            }

            // Update CPU temperatures
            const cpu1Temp = data.querySelector('CPU1').textContent;
            const cpu2Temp = data.querySelector('CPU2').textContent;
            document.getElementById('cpu1Temp').textContent = `${cpu1Temp} °C`;
            document.getElementById('cpu2Temp').textContent = `${cpu2Temp} °C`;

            // Update Power Consumption
            const power = data.querySelector('PowerConsumption').textContent;
            document.getElementById('powerConsumption').textContent = `${power} Watts`;
        })
        .catch(error => console.error('Error fetching sensor data:', error));
    }

setInterval(updateSensorData, 1000); // Update every second

function displayError(message, isError = true, persist = false) {
    const errorContainer = document.getElementById('error-container');
    const errorMessage = document.getElementById('error-message');
    errorMessage.textContent = message;

    // Remove the inline display style so that our class-based styles take effect
    errorContainer.style.display = '';

    // Apply different classes based on error or success
    if (isError) {
        errorContainer.classList.remove('success-container');
        errorContainer.classList.add('show-error', 'error-container');
    } else {
        errorContainer.classList.remove('error-container');
        errorContainer.classList.add('show-error', 'success-container');
    }

    // Hide the error container after a delay if not persistent
    if (!persist) {
        setTimeout(() => {
            errorContainer.classList.remove('show-error');
            // Reapply the inline display:none; style after hiding the error
            errorContainer.style.display = 'none';
        }, 3000);
    }

    // Allow the error container to be clicked to hide immediately
    errorContainer.addEventListener('click', () => {
    errorContainer.classList.remove('show-error');
  });
  
}


document.getElementById('automatic').addEventListener('click', function() {
    fetch('/setAutomatic', { method: 'POST' })
        .then(response => console.log('Automatic mode set'))
        .catch(error => console.error('Error:', error));
});

document.getElementById('manual').addEventListener('click', function() {
    fetch('/setManual', { method: 'POST' })
        .then(response => console.log('Manual mode set'))
        .catch(error => console.error('Error:', error));
});

// Disable fan speed buttons and slider initially
document.querySelectorAll('.manual, #customSpeed, .setSpeed').forEach(el => el.disabled = true);

// Function to toggle the fan speed buttons and slider
function toggleFanControls(enable) {
    document.querySelectorAll('.manual, #customSpeed, .setSpeed').forEach(el => el.disabled = !enable);
}

// Enable fan speed buttons and slider when "Manual" button is clicked
document.getElementById('manual').addEventListener('click', function() {
    toggleFanControls(true);
});


// Event listeners for fan speed buttons
document.querySelectorAll('.manual').forEach(button => {
    button.addEventListener('click', function() {
        const percentage = this.textContent.replace('%', '');
        fetch(`/setFanSpeed${percentage}`, { method: 'POST' })
            .then(response => console.log(`${percentage}% speed set`))
            .catch(error => console.error('Error:', error));
    });
});

// Event listener for "SET" button
document.querySelector('.setSpeed').addEventListener('click', function() {
    const sliderValue = document.getElementById('customSpeed').value;
    fetch(`/setFanSpeed?speed=${sliderValue}`, { method: 'POST' })
        .then(response => console.log(`Speed set to ${sliderValue}%`))
        .catch(error => console.error('Error:', error));
});