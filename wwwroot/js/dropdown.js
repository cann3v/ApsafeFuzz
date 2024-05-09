document.getElementById('selection').addEventListener('change', function() {
    var selectedOption = this.value;
    // Hide all inputs
    document.getElementById('input1').style.display = 'none';
    document.getElementById('input2').style.display = 'none';
    
    // Show specific inputs
    if (selectedOption === "option0") {
        document.getElementById('input1').style.display = 'none';
        document.getElementById('input2').style.display = 'none'
    } else if (selectedOption === 'option1') {
        document.getElementById('input1').style.display = 'block';
    } else if (selectedOption === 'option2') {
        document.getElementById('input2').style.display = 'block';
    }
});