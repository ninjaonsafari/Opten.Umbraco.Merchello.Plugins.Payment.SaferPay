; (function () {
	'use strict';

	angular.module("umbraco").controller("Merchello.Plugin.GatewayProviders.Dialogs.SaferPaySettingsController", gatewayProviderController);

	gatewayProviderController.$inject = ["$scope"];

	function gatewayProviderController($scope) {
		var vm = this,
			settingsKey = "OPTEN_TaxAndRoundingProviderSettings"; // Way to get from C#?

		vm.wasFormSubmitted = false;
		vm.settings = {};

		vm.saveSettings = saveSettings;
		vm.close = close;

		activate();


		// Private functions

		function activate() {
			var settingsString = $scope.dialogData.provider.extendedData.getValue(settingsKey);

			vm.wasFormSubmitted = false;
			vm.settings = settingsString ? JSON.parse(settingsString) : {};
		};

		function saveSettings(e) {
			e.preventDefault();

			$scope.dialogData.provider.extendedData.setValue(settingsKey, angular.toJson(vm.settings));

			$scope.submit($scope.dialogData);

			vm.wasFormSubmitted = true;
		};

		function close(e) {
			e.preventDefault();

			$scope.close();
		};
	};

}());