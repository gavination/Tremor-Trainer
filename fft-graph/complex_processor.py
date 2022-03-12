import numpy as np
import matplotlib.pyplot as plt


def convert_to_complex_array(file_contents):
    complex_nums = []
    for element in file_contents:
        newNum = complex(element["Real"], element["Imaginary"])
        complex_nums.append(newNum)
    return complex_nums

def create_frequency_signal(complex_numbers, time_in_seconds, graph_name):

    sample_rate = int(len(complex_numbers)/time_in_seconds)

    # Casts the fourier transform coefficients as a numpy array
    transformed_y = np.array(complex_numbers, dtype=complex)

    # Take the absolute value of the complex numbers for magnitude spectrum
    freq_magnitude = np.absolute(transformed_y)

    # Create frequency x-axis that will span up to sample_rate
    freq_axis = np.linspace(0, sample_rate, len(freq_magnitude))

    # Plot frequency domain
    plt.plot(freq_axis, freq_magnitude)
    plt.xlabel(f"Frequency (Hz) for {graph_name} with {sample_rate} samples/second")
    plt.ylabel("Magnitude")
    plt.xlim(0, 10)
    plt.ylim(0, 8)
    plt.show()