window.onload = function() {
    const toggleButton = document.getElementById('toggleConsole');
    const consoleOutput = document.querySelector('.console-output');
  
    toggleButton.addEventListener('click', function() {
      const isHidden = consoleOutput.style.display === 'none';
      consoleOutput.style.display = isHidden ? 'block' : 'none';
      toggleButton.textContent = isHidden ? 'Hide Console' : 'Show Console';
    });
  };

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

// Call updateSensorData at a set interval
setInterval(updateSensorData, 1000); // Update every second



  
  