// Helper functions
function toggleDisplay(element, isShow) {
    element.style.display = isShow ? 'block' : 'none';
}

function createElementWithText(xmlDoc, tagName, textContent) {
    const element = xmlDoc.createElement(tagName);
    element.textContent = textContent;
    return element;
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

function setupInputValidation() {
    const saveButton = document.getElementById('save');
    saveButton.addEventListener('click', () => {
        const ipAddressField = document.getElementById('ipAddress');
        const usernameField = document.getElementById('username');
        const passwordField = document.getElementById('password');

        if (!ipAddressField.checkValidity()) {
            displayError('Please enter a valid IP address.');
        } else if (!usernameField.checkValidity()) {
            displayError('Username must be up to 32 characters long and can include letters, numbers, and special characters.');
        } else if (!passwordField.checkValidity()) {
            displayError('Password must be up to 32 characters long and can include letters, numbers, and special characters.');
        } else {
            saveConfig();
        }
    });
}

function setupClearButton() {
    const clearButton = document.getElementById('clear');
    clearButton.addEventListener('click', () => {
        document.getElementById('ipAddress').value = '';
        document.getElementById('username').value = '';
        document.getElementById('password').value = '';
    });
}

function loadInitialConfig() {
    if (localStorage.getItem('darkMode') === 'true') {
        document.body.classList.add('dark-mode');
        document.getElementById('toggleDarkMode').textContent = 'Light Mode';
    }

    loadConfig();
}

// Call setup functions on DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
    loadInitialConfig();
    setupToggleButtons();
    setupInputValidation();
    setupClearButton();
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

function loadConfig() {
    fetch('/api/config')
        .then(response => response.text())
        .then(data => {
            const parser = new DOMParser();
            const xmlDoc = parser.parseFromString(data, "text/xml");
            
            document.getElementById('ipAddress').value = xmlDoc.getElementsByTagName('IpAddress')[0].childNodes[0].nodeValue;
            document.getElementById('username').value = xmlDoc.getElementsByTagName('Username')[0].childNodes[0].nodeValue;
            document.getElementById('password').value = xmlDoc.getElementsByTagName('Password')[0].childNodes[0].nodeValue;
        })
        .catch(error => displayError('Failed to load settings from the server.'));
}

document.getElementById('save').addEventListener('click', function() {
    var ipAddressField = document.getElementById('ipAddress');
    var usernameField = document.getElementById('username');
    var passwordField = document.getElementById('password');
  
    if (!ipAddressField.checkValidity()) {
      displayError('Please enter a valid IP address.');
    } else if (!usernameField.checkValidity()) {
      displayError('Username must be up to 32 characters long and can include letters, numbers, and special characters.');
    } else if (!passwordField.checkValidity()) {
      displayError('Password must be up to 32 characters long and can include letters, numbers, and special characters.');
    } else {
      // Proceed with saving the configuration
      saveConfig();
    }
  });

  
// Save settings to the server
function saveConfig() {
    const ip = document.getElementById('ipAddress').value;
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    if (!ip || !username || !password) {
        displayError('All fields are required.');
        return;
    }

    const settings = new XMLSerializer().serializeToString(createSettingsXml(ip, username, password));
    
    fetch('/api/config', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/xml',
        },
        body: settings
    })
    .then(response => {
        if (response.ok) {
            displayError('Settings saved successfully.', false);
        } else {
            throw new Error('Server responded with an error.');
        }
    })
    .catch(error => displayError('Failed to save settings to the server.'));
}


// Helper function to create XML from settings
function createSettingsXml(ip, username, password) {
    const xmlDoc = document.implementation.createDocument(null, "Settings");
    xmlDoc.documentElement.appendChild(createElementWithText(xmlDoc, 'IpAddress', ip));
    xmlDoc.documentElement.appendChild(createElementWithText(xmlDoc, 'Username', username));
    xmlDoc.documentElement.appendChild(createElementWithText(xmlDoc, 'Password', password));
    return xmlDoc;
}


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

